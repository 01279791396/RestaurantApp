// Small progressive enhancements for phones. Nothing here is required for the site to work:
// if this file fails to load, every page still renders and behaves normally.
(function () {
    'use strict';

    // 1) Tables -> cards on phones.
    //    The stacking itself is pure CSS (see "Tables -> cards on phones" in site.css and it only
    //    applies below 768px). All we do here is copy each column header into a data-label on its
    //    cells, so the CSS can print "label: value" on every row. This works for every table in
    //    the project (customer + admin) without editing each view.
    function stackTables() {
        var tables = document.querySelectorAll('table.table');
        tables.forEach(function (table) {
            if (table.classList.contains('no-stack')) return;

            var heads = Array.prototype.map.call(
                table.querySelectorAll('thead th'),
                function (th) { return th.textContent.trim(); });
            if (!heads.length) return;

            table.querySelectorAll('tbody tr').forEach(function (tr) {
                Array.prototype.forEach.call(tr.children, function (cell, i) {
                    if (!cell.hasAttribute('data-label')) {
                        cell.setAttribute('data-label', heads[i] || '');
                    }
                });
            });

            table.classList.add('table-stack');
            var wrap = table.closest('.table-responsive');
            if (wrap) wrap.classList.add('stack-wrap');
        });
    }

    // 2) Admin pages print raw English enum names for the order status (Pending, Delivered...).
    //    Show the Arabic text instead, only for exact matches inside badges / dropdown options.
    var statusAr = {
        'Pending': 'قيد الانتظار',
        'Accepted': 'تم القبول',
        'Preparing': 'جاري التحضير',
        'OutForDelivery': 'مع مندوب التوصيل',
        'Delivered': 'تم التسليم',
        'Cancelled': 'ملغي'
    };
    function translateStatuses() {
        document.querySelectorAll('.badge, select option').forEach(function (el) {
            if (el.children.length) return;
            var key = el.textContent.trim();
            if (Object.prototype.hasOwnProperty.call(statusAr, key)) {
                el.textContent = statusAr[key];
            }
        });
    }

    // 3) Highlight the current page in the bottom bar.
    function markActiveBarLink() {
        var path = location.pathname.toLowerCase().replace(/\/+$/, '') || '/';
        document.querySelectorAll('.mobile-bottom-bar > a').forEach(function (a) {
            var p = (a.pathname || '').toLowerCase().replace(/\/+$/, '') || '/';
            if (p === path || (p !== '/' && path.indexOf(p + '/') === 0)) {
                a.classList.add('active');
            }
        });
    }

    // 4) Admin bar "more" button opens the full navigation menu.
    function wireAdminMore() {
        var btn = document.getElementById('adminMoreBtn');
        var nav = document.getElementById('navMain');
        if (!btn || !nav || !window.bootstrap) return;
        btn.addEventListener('click', function () {
            window.scrollTo({ top: 0, behavior: 'smooth' });
            window.bootstrap.Collapse.getOrCreateInstance(nav).toggle();
        });
    }

    // 5) Menu page: highlight the category chip of the section currently on screen.
    function wireCategoryChips() {
        var chips = document.querySelectorAll('.cat-chip');
        if (!chips.length || !('IntersectionObserver' in window)) return;

        var byId = {};
        chips.forEach(function (chip) { byId[chip.getAttribute('href').slice(1)] = chip; });

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (!entry.isIntersecting) return;
                chips.forEach(function (c) { c.classList.remove('active'); });
                var chip = byId[entry.target.id];
                if (chip) {
                    chip.classList.add('active');
                    var bar = chip.parentElement;
                    if (bar) bar.scrollTo({ left: chip.offsetLeft - bar.clientWidth / 2 + chip.clientWidth / 2, behavior: 'smooth' });
                }
            });
        }, { rootMargin: '-30% 0px -60% 0px' });

        Object.keys(byId).forEach(function (id) {
            var target = document.getElementById(id);
            if (target) observer.observe(target);
        });
    }

    function init() {
        stackTables();
        translateStatuses();
        markActiveBarLink();
        wireAdminMore();
        wireCategoryChips();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
