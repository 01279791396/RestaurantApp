// Bump this number whenever css/js change so phones drop the old copies.
const CACHE_NAME = 'restaurant-app-shell-v2';
const APP_SHELL = [
    '/manifest.json',
    '/css/site.css',
    '/js/cart.js',
    '/js/site-mobile.js',
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

    // CSS and JS: network-first, so a new design shows up right after a deploy instead of
    // being stuck on the old cached copy. The cache is only the offline fallback.
    if (request.destination === 'style' || request.destination === 'script') {
        event.respondWith(
            fetch(request).then(function (response) {
                if (response.ok) {
                    var copy = response.clone();
                    caches.open(CACHE_NAME).then(function (cache) { cache.put(request, copy); });
                }
                return response;
            }).catch(function () {
                return caches.match(request);
            })
        );
        return;
    }

    // Other static assets (icons, images, manifest): cache-first.
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
