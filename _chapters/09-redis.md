---
layout: chapter
title: "Redis"
number: 9
part: 3
---

[Redis](https://redis.io/) is an open-source, in-memory data store commonly used as a database, cache, and message broker. It supports data structures such as strings, hashes, lists, sets, and more, and is known for its high performance and simplicity.

forks:

- [Valkey](https://valkey.io/): A community-driven fork supported by the Linux Foundation, aiming to maintain an open-source alternative.
- [Redict](https://redict.io/): Another fork focused on preserving the original open-source spirit of Redis.

These forks are compatible with Redis and are intended to provide drop-in replacements for users who require a fully open-source solution.

Other redis compatible solutions:

- [DragonflyDB](https://www.dragonflydb.io/)

## Ubuntu Install

```bash
sudo apt update
sudo apt install redis-server
sudo cp /etc/redis/redis.conf /etc/redis/redis.conf.backup

sudo ufw allow ssh
sudo ufw allow redis
sudo ufw allow 6380/tcp
sudo ufw enable
sudo ufw status
```
### Add redis password
/etc/redis/redis.conf

add

```text
user default on >[PLACEHOLDER] ~* +@all
acl-pubsub-default allchannels
```

### Connect to password protected redis server

```bash
redis-cli -h 127.0.0.1 -p 6379
AUTH [PLACEHOLDER]
```

### expose to the network

sudo vim /etc/redis/redis.conf

```ini
bind 0.0.0.0
```

### Restart on failure
Ensure restart on failure is enabled

```bash
sudo cat /lib/systemd/system/redis-server.service
```

2. **Look for Restart Policies**:
Within the service file, look for directives related to the restart policy. Common directives include `Restart` and `RestartSec`.

Example:
```ini
[Service]
Type=notify
ExecStart=/usr/bin/redis-server /etc/redis/redis.conf
ExecStop=/usr/bin/redis-shutdown
User=redis
Group=redis
RuntimeDirectory=redis
RuntimeDirectoryMode=2755
PIDFile=/run/redis/redis-server.pid
TimeoutStopSec=0
Restart=on-failure
RestartSec=5
```

3. **Modify the Service Configuration if Necessary**:
If the `Restart` directive is not set or you want to customize it, you can create a systemd override file to modify the service configuration without changing the original service file.

```bash
sudo systemctl edit redis-server
```

This command opens an editor where you can add or override directives. For example, to ensure the service restarts on failure, you can add:

```ini
[Unit]
StartLimitIntervalSec=0  # Disable the limit on the time window
StartLimitBurst=0        # Disable the limit on the number of restart attempts

[Service]
Restart=always           # Always restart the service
RestartSec=10            # Wait 10 seconds before restarting
```

4. **Reload Systemd and Restart the Service**:
After making changes, reload the systemd configuration and restart the Redis service to apply the changes.

```bash
sudo systemctl daemon-reload
sudo systemctl restart redis-server
```

5. **Verify the Configuration**:
Finally, you can verify that the service is configured correctly by checking its status.

```bash
sudo systemctl status redis-server
```

### TLS Connection Support

If TLS support is enabled the client connection strings must be configured to use it.

#### 1. Generate SSL/TLS Certificates

You can generate self-signed certificates for testing purposes or obtain certificates from a trusted Certificate Authority (CA) for production use. Here’s how to generate self-signed certificates using OpenSSL:

```bash
# Create a directory to store the certificates
mkdir -p /etc/redis/ssl
cd /etc/redis/ssl

# Generate a private key
openssl genrsa -out redis-server.key 2048

# Generate a self-signed certificate
openssl req -new -x509 -key redis-server.key -out redis-server.crt -days 3652

# Generate a private key for the client
openssl genrsa -out redis-client.key 2048

# Generate a certificate signing request (CSR) for the client
openssl req -new -key redis-client.key -out redis-client.csr

# Generate a self-signed certificate for the client
openssl x509 -req -in redis-client.csr -CA redis-server.crt -CAkey redis-server.key -CAcreateserial -out redis-client.crt -days 3652

chgrp -R redis /etc/redis/ssl
chown -R redis /etc/redis/ssl
```

#### 2. Configure Redis to Use TLS

Edit the Redis configuration file (`/etc/redis/redis.conf`) to enable TLS and specify the paths to your certificates and keys.

```ini
# Non-TLS port, disable for enhanced security
port 6379

# Enable TLS
tls-port 6380

# Specify the paths to the certificates and keys
tls-cert-file /etc/redis/ssl/redis-server.crt
tls-key-file /etc/redis/ssl/redis-server.key
tls-ca-cert-file /etc/redis/ssl/redis-server.crt

# Optional: Require clients to authenticate using a client certificate
tls-auth-clients no
```

#### 3. Restart Redis Server

After making these changes, restart the Redis server to apply the new configuration.

```bash
sudo systemctl restart redis-server
```

## Session

To use Redis for web sessions in C#, add the `Microsoft.Extensions.Caching.StackExchangeRedis` NuGet package to your ASP.NET Core project. In `Program.cs`, configure session state to use Redis as the backing store:

```cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379,password=yourpassword";
    options.InstanceName = "SampleInstance";
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

app.UseSession();
```

You can then use `HttpContext.Session` to store and retrieve session data. Redis will persist session state across web server restarts and scale-out scenarios.

## Cache

To use Redis as a distributed cache in a C# ASP.NET Core application, add the `Microsoft.Extensions.Caching.StackExchangeRedis` NuGet package. Configure Redis in `Program.cs`:

```cs
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379,password=yourpassword";
    options.InstanceName = "SampleInstance";
});
```

You can then use `IDistributedCache` to store and retrieve cached data:

```cs
public class MyController : Controller
{
    private readonly IDistributedCache _cache;

    public MyController(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IActionResult> Index()
    {
        var value = await _cache.GetStringAsync("myKey");
        if (value == null)
        {
            value = "Hello from Redis cache!";
            await _cache.SetStringAsync("myKey", value, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            });
        }
        return Content(value);
    }
}
```

This enables fast, centralized caching for web applications, improving performance and scalability.

## Publish and Subscribe

**Publish/Subscribe (Pub/Sub)** is a messaging pattern where senders (publishers) send messages to channels without knowing who will receive them, and receivers (subscribers) listen for messages on those channels. Redis provides built-in support for Pub/Sub, enabling real-time messaging between distributed components.

### Using Redis Pub/Sub in C#

To use Redis Pub/Sub in a C# application, add the `StackExchange.Redis` NuGet package. You can then publish and subscribe to messages as shown below:

```cs
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("localhost:6379,password=yourpassword");
var pubsub = redis.GetSubscriber();

// Subscribe to a channel
pubsub.Subscribe("notifications", (channel, message) => {
    Console.WriteLine($"Received: {message}");
});

// Publish a message to the channel
pubsub.Publish("notifications", "Hello from publisher!");

// Keep the application running to receive messages
Console.ReadLine();
```

This allows different parts of your application (or different applications) to communicate in real time using Redis channels.

## Example: Redis Pub/Sub with Web Frontend and Worker Services

This example demonstrates a simple architecture where:

- A web frontend allows users to submit work (e.g., a "task").
- The frontend publishes the task to a Redis channel.
- One or more worker services (in separate processes) subscribe to the channel, process the task, and publish a completion message to another channel.
- The frontend listens for completion messages and notifies the user when their task is done.

### 1. Web Frontend (ASP.NET Core + JavaScript)

**Backend Controller (C#):**

```cs
// Controller to accept user input and publish to Redis
[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    public TasksController(IConnectionMultiplexer redis) => _redis = redis;

    [HttpPost]
    public async Task<IActionResult> SubmitTask([FromBody] TaskRequest request)
    {
        var id = Guid.NewGuid().ToString();
        var pub = _redis.GetSubscriber();
        await pub.PublishAsync("tasks", $"{id}:{request.Payload}");
        return Ok(new { taskId = id });
    }
}

public class TaskRequest
{
    public string Payload { get; set; }
}
```

**Frontend (HTML + JavaScript):**

```html
<input id="taskInput" placeholder="Enter task" />
<button onclick="submitTask()">Submit</button>
<div id="status"></div>
<script>
let taskId = null;
function submitTask() {
    const payload = document.getElementById('taskInput').value;
    fetch('/api/tasks', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ payload })
    })
    .then(r => r.json())
    .then(data => {
        taskId = data.taskId;
        document.getElementById('status').innerText = 'Task submitted. Waiting for completion...';
    });
}

// Listen for completion via WebSocket (SignalR or custom implementation)
const ws = new WebSocket('wss://yourserver/ws');
ws.onmessage = function(event) {
    const msg = JSON.parse(event.data);
    if (msg.taskId === taskId) {
        document.getElementById('status').innerText = 'Task complete: ' + msg.result;
    }
};
</script>
```

**Note:** The backend should push completion messages to the frontend via WebSocket (e.g., using SignalR).

### 2. Worker Service (C# Console App)

```cs
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("localhost:6379,password=yourpassword");
var sub = redis.GetSubscriber();

sub.Subscribe("tasks", async (channel, message) => {
    var parts = message.ToString().Split(':', 2);
    var taskId = parts[0];
    var payload = parts[1];

    // Simulate work
    await Task.Delay(2000);
    var result = payload.ToUpperInvariant();

    // Publish completion
    await sub.PublishAsync("tasks-complete", $"{taskId}:{result}");
});

Console.WriteLine("Worker running. Press Enter to exit.");
Console.ReadLine();
```

### 3. Completion Notification Service

A background service (e.g., in your ASP.NET Core app) subscribes to `"tasks-complete"` and pushes updates to the frontend via WebSocket/SignalR.

```cs
public class CompletionNotifier : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IHubContext<NotifyHub> _hub;
    public CompletionNotifier(IConnectionMultiplexer redis, IHubContext<NotifyHub> hub)
    {
        _redis = redis; _hub = hub;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sub = _redis.GetSubscriber();
        return sub.SubscribeAsync("tasks-complete", async (ch, msg) => {
            var parts = msg.ToString().Split(':', 2);
            var taskId = parts[0];
            var result = parts[1];
            await _hub.Clients.All.SendAsync("TaskCompleted", new { taskId, result });
        });
    }
}
```

**SignalR Hub:**

```cs
public class NotifyHub : Hub { }
```

**Frontend SignalR Listener:**

```js
const connection = new signalR.HubConnectionBuilder().withUrl("/notifyhub").build();
connection.on("TaskCompleted", function(msg) {
    if (msg.taskId === taskId) {
        document.getElementById('status').innerText = 'Task complete: ' + msg.result;
    }
});
connection.start();
```

This pattern enables real-time user feedback for background work using Redis Pub/Sub, web frontend, and worker services.

## Work and Message Queue with Redis in C#

A work queue (also known as a message queue or task queue) allows producers to enqueue work items, and one or more consumers (workers) to process them asynchronously. Redis is commonly used for this pattern using its list commands (`LPUSH`/`RPUSH` to enqueue, `BRPOP`/`BLPOP` to dequeue).

### Producer Example (Enqueue Work)

```cs
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("localhost:6379,password=yourpassword");
var db = redis.GetDatabase();

// Enqueue a work item (e.g., a JSON string or simple string)
await db.ListRightPushAsync("work-queue", "do_something:12345");
```

### Consumer Example (Worker)

```cs
using StackExchange.Redis;

var redis = ConnectionMultiplexer.Connect("localhost:6379,password=yourpassword");
var db = redis.GetDatabase();

while (true)
{
    // Atomically move an item off the work queue and onto a processing
    // queue, so the item is not lost if this worker crashes mid-job.
    var result = await db.ListRightPopLeftPushAsync("work-queue", "processing-queue");
    if (!result.HasValue)
    {
        // Nothing waiting.  StackExchange.Redis has no blocking pop, so
        // back off briefly instead of spinning on an empty queue.
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        continue;
    }

    var workItem = result.ToString();
    Console.WriteLine($"Processing: {workItem}");

    // Do work here...

    // Remove from processing-queue only after successful processing
    await db.ListRemoveAsync("processing-queue", workItem);
}
```

**Notes:**
- Use `ListRightPushAsync` to enqueue work and `ListRightPopLeftPushAsync` to dequeue it.
- StackExchange.Redis deliberately does not expose the blocking `BRPOP`/`BLPOP` commands, because a blocked connection would stall every other operation sharing that multiplexer. Poll with a short delay, as above.
- The "processing" queue is what makes this reliable. Anything left sitting in it belongs to a worker that died and can be moved back to `work-queue` by a reaper process.
- For more advanced scenarios, consider libraries like [Hangfire](https://www.hangfire.io/).
