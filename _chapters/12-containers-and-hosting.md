---
layout: chapter
title: "Containers, nginx and Kubernetes"
number: 12
part: 4
---

## Containers - Docker

A Docker container is a lightweight, portable, and self-sufficient unit that packages an application and all its dependencies, ensuring consistent execution across different environments. Containers are isolated from each other and the host system, making deployment and scaling straightforward.

### Example: Dockerfile for a .NET Web Application

```dockerfile
# Use the official .NET SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore first, so the restore layer is
# cached and only re-runs when a dependency actually changes.
COPY *.sln .
COPY YourWebApp/*.csproj ./YourWebApp/
RUN dotnet restore

COPY . .
RUN dotnet publish YourWebApp -c Release -o /app --no-restore

# Use the ASP.NET runtime image for hosting
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
EXPOSE 8080
ENTRYPOINT ["dotnet", "YourWebApp.dll"]
```

Note the port. The official .NET container images run as a non root user and listen on 8080, not 80, since .NET 8. A non root user cannot bind a port below 1024.

### Build and Publish with Docker Buildx and SBOM

one time setup

```bash
docker buildx create --use --name=buildkit-container --driver=docker-container
```

regular builds

```bash
# Build the image with SBOM (Software Bill of Materials) generation
docker buildx build --sbom=true -t yourusername/yourwebapp:latest .

# Publish (push) the image to a container registry (e.g., Docker Hub)
docker push yourusername/yourwebapp:latest
```

The `--sbom=true` flag generates a Software Bill of Materials, providing transparency into the components included in the image for improved security and compliance.

## nginx

[nginx](https://nginx.org/) is commonly placed in front of an ASP.NET Core application as a reverse proxy. Kestrel, the built in web server, is perfectly capable of serving traffic directly, but a proxy in front gives you TLS termination, a single entry point for several applications on one host, and static file serving without touching the runtime.

Install it on ubuntu.

```bash
sudo apt-get update
sudo apt-get install nginx
sudo systemctl enable --now nginx
```

Configure a site at `/etc/nginx/sites-available/yourapp`, then symlink it into `sites-enabled`.

```nginx
server {
    listen 80;
    server_name yourapp.example.com;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;

        # required for websockets, SignalR, and Blazor Server
        proxy_set_header Upgrade    $http_upgrade;
        proxy_set_header Connection $connection_upgrade;

        proxy_set_header Host              $host;
        proxy_set_header X-Real-IP         $remote_addr;
        proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

`$connection_upgrade` is not built in and must be defined in the `http` block, usually in `/etc/nginx/nginx.conf`.

```nginx
map $http_upgrade $connection_upgrade {
    default upgrade;
    ''      close;
}
```

Enable the site and reload. `nginx -t` checks the configuration before you apply it, which is worth doing every time.

```bash
sudo ln -s /etc/nginx/sites-available/yourapp /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

### Tell ASP.NET Core it is behind a proxy

Without this step the application sees every request as coming from `127.0.0.1` over plain http. Logging, rate limiting, and any redirect to https will all be wrong. `UseForwardedHeaders` must run before anything that depends on the scheme or client address.

```bash
dotnet add package Microsoft.AspNetCore.HttpOverrides
```

```cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // Only trust the proxy in front of us.  Clearing these lists
    // without setting KnownProxies would trust any caller's headers.
    options.KnownProxies.Add(System.Net.IPAddress.Parse("127.0.0.1"));
});

var app = builder.Build();

app.UseForwardedHeaders();
```

### TLS with Let's Encrypt

Terminate TLS at nginx rather than in Kestrel. certbot obtains the certificate, edits the site configuration, and installs a renewal timer.

```bash
sudo apt-get install certbot python3-certbot-nginx
sudo certbot --nginx -d yourapp.example.com
```

### Run the application as a service

systemd keeps the application running and restarts it after a crash or a reboot. Create `/etc/systemd/system/yourapp.service`.

```ini
[Unit]
Description=Your ASP.NET Core application
After=network.target

[Service]
WorkingDirectory=/var/www/yourapp
ExecStart=/usr/bin/dotnet /var/www/yourapp/YourWebApp.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now yourapp
sudo systemctl status yourapp
```

Binding to `127.0.0.1` rather than `0.0.0.0` means the application is only reachable through nginx, not directly from the network.

## Kubernetes

```bash
# use multiple kubeconfig files at the same time and view merged config
KUBECONFIG=~/.kube/config:~/.kube/kubconfig2
kubectl config view
kubectl config get-contexts
kubectl config current-context
kubectl get nodes
kubectl get namespaces
kubectl -n theNamespace get all
kubectl -n theNamespace get pods
kubectl -n theNamespace get deployments
kubectl -n theNamespace get service
kubectl -n theNamespace get ingress
kubectl -n theNamespace describe deployment theDeployment

# create a new pod yaml file.  Edit the pod.yaml file to your needs.
kubectl run nginx --image=nginx --dry-run=client -o yaml > pod.yaml
kubectl create -f pod.yaml
```

Further reading:

- [kubectl cheat sheet](https://kubernetes.io/docs/reference/kubectl/cheatsheet/)
- [Kubernetes Health Checks and Resource Reservations](/posts/2023/03/27/kubernetes-health-checks-and-resource-reservations.html)
