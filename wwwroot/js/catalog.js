document.querySelectorAll('.translate-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
        const row = btn.closest('tr');
        const lang = row.querySelector('.lang-select').value;
        const menuId = btn.dataset.menuId;
        const restaurantId = btn.dataset.restaurantId;
        const text = btn.dataset.text;
        const resultSpan = row.querySelector('.translation-result');

        btn.disabled = true;
        btn.textContent = '...';
        resultSpan.textContent = '';

        fetch(`/Menu/Translate?menuId=${encodeURIComponent(menuId)}&restaurantId=${encodeURIComponent(restaurantId)}&text=${encodeURIComponent(text)}&language=${encodeURIComponent(lang)}`)
            .then(function (res) { return res.json(); })
            .then(function (data) {
                resultSpan.textContent = data.translated;
                btn.disabled = false;
                btn.textContent = 'Translate';
            })
            .catch(function () {
                resultSpan.textContent = 'Translation failed.';
                resultSpan.classList.replace('text-success', 'text-danger');
                btn.disabled = false;
                btn.textContent = 'Translate';
            });
    });
});
