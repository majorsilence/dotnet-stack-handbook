---
layout: chapter
title: "JavaScript in the Browser"
number: 13
part: 4
---

JavaScript is a lightweight, interpreted programming language primarily used for client-side web development. It enables dynamic content, user interaction, and DOM manipulation in browsers. This guide focuses on plain JavaScript for frontend tasks, avoiding dependencies and TypeScript for simplicity and maintainability.

## Use htmx to Avoid Complicated JavaScript

Use [htmx](https://htmx.org/) when you want to add dynamic, interactive features to your web application without writing or maintaining large amounts of custom JavaScript.

> htmx gives you access to AJAX, CSS Transitions, WebSockets and Server Sent Events directly in HTML, using attributes, so you can build modern user interfaces with the simplicity and power of hypertext

## fetch

Call a service using post with fetch api. These examples uses helper functions that are defined in [Helper functions](#helper-functions) below.

### call fetch - form-urlencoded

Use a custom **serialize** helper method to transform an javascript object (json) to an form url encoded format.

Example:

> ?test_param=test value&another_param=another value

```js
function PostFormUrlEncoded(msg) {
    var data = serialize({
        test_param: "test value",
        another_param: "another value",
    });

    return fetch(site + "/some/url", {
        method: "POST",
        mode: "cors",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
        },
        body: data,
    });
}

PostFormUrlEncoded("My comment")
    .then(status_helper)
    .then(json_helper)
    .then(function (data) {
        console.log(data);
    })
    .catch(function (error) {
        console.log(error);
    });
```

### call fetch - application/json

The content type **application/json** can use the builtin method **JSON.stringify** to send data.

```js
const site = "https://example.com"; // Define your site URL

function PostJson(msg) {
  var data = JSON.stringify({
    test_param: "test value",
    another_param: "another value",
  });

  return fetch(site + "/some/url", {
    method: "POST",
    mode: "cors",
    headers: {
      "Content-Type": "application/json",
    },
    body: data,
  });
}

PostJson("My comment")
  .then(status_helper)
  .then(json_helper)
  .then(function (data) {
    console.log(data);
  })
  .catch(function (error) {
    console.log(error);
  });
```

### Helper functions {#helper-functions}

These helper functions implement some boiler plate code that will almost always be needed.

```js
function status_helper(response) {
  if (response.status >= 200 && response.status < 300) {
    return Promise.resolve(response);
  } else {
    return Promise.reject(new Error(response.statusText));
  }
}

function json_helper(response) {
  return response.json();
}

function serialize(obj, prefix) {
  if (prefix === void 0) {
    prefix = null;
  }
  var str = [],
    p;
  for (p in obj) {
    if (Object.prototype.hasOwnProperty.call(obj, p)) {
      var k = prefix ? prefix + "[" + p + "]" : p,
        v = obj[p];
      str.push(
        v !== null && typeof v === "object"
          ? serialize(v, k)
          : encodeURIComponent(k) + "=" + encodeURIComponent(v)
      );
    }
  }
  return str.join("&");
}
```

## async/await

Async and await support is built upon javascript promises. The following example is a slight modification on the PostJson example

Notice how the DownloadPage function is a GET and does not have a mode, headers, or body. A body must not be set on a GET but the other properties are setable. DownloadPage returns the response.text().

In contrast the PostJson function is a POST and sets the mode to cors, headers, and a body. PostJson returns the response.json().

```js
async function DownloadPage(url) {
  const response = await fetch(url, {
    method: "GET",
  });

  if (response.status < 200 || response.status > 299) {
    throw new Error(response.status);
  }

  return response.text();
}

async function PostJson(url, msg) {
  var data = JSON.stringify({
    test_param: "test value",
    another_param: "another value",
  });

  const response = await fetch(url, {
    method: "POST",
    mode: "cors",
    headers: {
      "Content-Type": "application/json",
    },
    body: data,
  });

  if (response.status < 200 || response.status > 299) {
    throw new Error(response.status);
  }

  return response.json();
}

PostJson("https://majorsilence.com/non/existing/post/page", "My comment")
  .then(function (data) {
    console.log(data);
  })
  .catch(function (error) {
    console.log(error);
  });

DownloadPage("https://majorsilence.com")
  .then(function (data) {
    console.log(data);
  })
  .catch(function (error) {
    console.log(error);
  });
```

## jQuery

Avoid for new work.

## Kendo UI

Avoid unless advanced controls are required.
