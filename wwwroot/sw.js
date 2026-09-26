const CACHE_NAME = 'restaurant-app-shell-v1';
const APP_SHELL = [
    '/manifest.json',
    '/css/site.css',
    '/icons/icon-192.png',
    '/icons/icon-512.png',
    '/offline.html'
];

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_NAME).then(function (cache) {
            return cache.addAll(APP_SHELL);
        })
    );
    self.skipWaiting();
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(
                keys.filter(function (key) { return key !== CACHE_NAME; })
                    .map(function (key) { return caches.delete(key); })
            );
        })
    );
    self.clients.claim();
});

self.addEventListener('fetch', function (event) {
    var request = event.request;

    // Only handle same-origin GET requests — never intercept POST (checkout, login, admin
    // actions, etc.) or cross-origin requests (Bootstrap/jQuery CDNs), so nothing about the
    // app's actual behavior changes, only what happens when there's no network.
    if (request.method !== 'GET' || new URL(request.url).origin !== self.location.origin) {
        return;
    }

    // Page navigations: menu, orders, admin pages are all server-rendered and depend on
    // live data (stock, prices, order status), so always go to the network first. Only fall
    // back to a friendly offline page when there's genuinely no connection.
    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(function () {
                return caches.match('/offline.html');
            })
        );
        return;
    }

    // Static assets (css, icons, manifest): cache-first, since these rarely change and this
    // is what actually makes the shell load instantly / work offline.
    event.respondWith(
        caches.match(request).then(function (cached) {
            return cached || fetch(request).then(function (response) {
                if (response.ok) {
                    var copy = response.clone();
                    caches.open(CACHE_NAME).then(function (cache) { cache.put(request, copy); });
                }
                return response;
            }).catch(function () {
                // Not cached and no network — nothing sensible to return for an arbitrary asset.
                return cached;
            });
        })
    );
});
