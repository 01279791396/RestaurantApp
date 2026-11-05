(function () {
    function updateBadges(count) {
        ['cartBadge', 'cartBadgeMobile'].forEach(function (id) {
            var el = document.getElementById(id);
            if (el) el.textContent = count;
        });
    }

    document.addEventListener('submit', function (e) {
        var form = e.target.closest('.add-to-cart-form');
        if (!form) return;
        e.preventDefault();

        var btn = form.querySelector('button[type=submit]');
        var oldText = btn ? btn.innerHTML : '';
        if (btn) { btn.disabled = true; btn.innerHTML = '...'; }

        fetch(form.action, {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
        .then(function (r) {
            if (!r.ok) throw new Error('bad');
            return r.json();
        })
        .then(function (data) {
            var count = data.count ?? data.cartCount ?? data.total;
            if (count !== undefined) updateBadges(count);
            if (btn) {
                btn.innerHTML = '✓ تمت الإضافة';
                setTimeout(function () { btn.innerHTML = oldText; btn.disabled = false; }, 1200);
            }
        })
        .catch(function () {
            // لو حصل أي خطأ، ارجع للإرسال العادي
            form.submit();
        });
    });
})();