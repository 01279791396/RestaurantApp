// Progressive enhancement: any form with class "add-to-cart-form" is submitted via fetch
// instead of a normal page POST, so adding an item to the cart doesn't reload the whole
// page. If fetch fails for any reason (network, JS error), we fall back to a real form
// submit so the add-to-cart action still goes through.
function updateCartBadges(count) {
    ['cartBadge', 'cartBadgeMobile'].forEach(function (id) {
        var badge = document.getElementById(id);
        if (!badge) return;
        badge.textContent = count;
        badge.style.display = count > 0 ? (id === 'cartBadge' ? 'inline-block' : 'block') : 'none';
    });
}

document.addEventListener('submit', function (event) {
    var form = event.target;
    if (!(form instanceof HTMLFormElement) || !form.classList.contains('add-to-cart-form')) {
        return;
    }

    event.preventDefault();

    var button = form.querySelector('button[type="submit"]');
    var originalLabel = button ? button.textContent : null;
    if (button) {
        button.disabled = true;
        button.textContent = '...';
    }

    fetch(form.action, {
        method: 'POST',
        body: new FormData(form),
        credentials: 'same-origin',
        headers: { 'X-Requested-With': 'XMLHttpRequest' }
    })
        .then(function (response) { return response.json(); })
        .then(function (data) {
            if (typeof data.cartCount === 'number') {
                updateCartBadges(data.cartCount);
            }

            if (button) {
                button.textContent = data.success ? '✓ اتضافت' : 'خطأ';
                setTimeout(function () {
                    button.textContent = originalLabel;
                    button.disabled = false;
                }, 1200);
            }

            if (!data.success && data.message) {
                alert(data.message);
            }
        })
        .catch(function () {
            // Something went wrong with the AJAX call itself (not the add-to-cart logic) —
            // fall back to a normal form submit rather than silently doing nothing.
            if (button) {
                button.disabled = false;
                button.textContent = originalLabel;
            }
            form.submit();
        });
});
