document.addEventListener('DOMContentLoaded', function () {
    const reportTypeSelect = document.getElementById('reportType');
    const templateSelect = document.getElementById('templateId');
    const allTemplates = Array.from(templateSelect.options).filter(opt => opt.value !== "");

    // Элементы периода
    const yearGroup = document.getElementById('archive-year-group');
    const monthGroup = document.getElementById('archive-month-group');
    const quarterGroup = document.getElementById('archive-quarter-group');
    const halfGroup = document.getElementById('archive-half-group');
    const periodGroups = [yearGroup, monthGroup, quarterGroup, halfGroup];

    // Функция для показа/скрытия полей периода
    function updatePeriodFields() {
        const selectedOption = templateSelect.selectedOptions[0];
        const periodType = selectedOption ? selectedOption.getAttribute('data-period-type') : null;

        // Скрыть все группы
        periodGroups.forEach(group => group.style.display = 'none');

        if (!periodType) return;

        // Показать год для всех типов
        yearGroup.style.display = 'block';

        // Показать соответствующие поля периода
        switch (periodType) {
            case 'Monthly':
                monthGroup.style.display = 'block';
                break;
            case 'Quarterly':
                quarterGroup.style.display = 'block';
                break;
            case 'HalfYearly':
                halfGroup.style.display = 'block';
                break;
            // Для Yearly показываем только год
        }
    }

    // Функция для фильтрации шаблонов по типу отчета
    function filterTemplatesByType(type) {
        const currentTemplateId = templateSelect.value;

        templateSelect.innerHTML = '<option value="">Все</option>';
        const filtered = allTemplates.filter(opt => opt.dataset.type === type || type === "");
        filtered.forEach(opt => templateSelect.appendChild(opt));

        // Восстановить выбранный шаблон, если он есть в отфильтрованном списке
        if (currentTemplateId) {
            const optionToSelect = templateSelect.querySelector(`option[value="${currentTemplateId}"]`);
            if (optionToSelect) optionToSelect.selected = true;
        }

        updatePeriodFields();
    }

    // Инициализация при загрузке
    function initializeFilters() {
        // Применить фильтр по типу отчета
        filterTemplatesByType(reportTypeSelect.value);

        // Показать соответствующие поля периода
        updatePeriodFields();

        // Убедиться, что год виден, если выбран шаблон с периодом
        const selectedTemplate = templateSelect.selectedOptions[0];
        if (selectedTemplate && selectedTemplate.value !== "") {
            yearGroup.style.display = 'block';
        }
    }

    // Обработчики событий
    reportTypeSelect.addEventListener('change', () => {
        filterTemplatesByType(reportTypeSelect.value);
    });

    templateSelect.addEventListener('change', updatePeriodFields);

    // Сброс фильтров
    document.getElementById('resetFilters').addEventListener('click', () => {
        const form = document.querySelector('form[method="get"]');
        form.querySelectorAll('input[type="text"], input[type="date"], input[type="number"]').forEach(input => input.value = '');
        form.querySelectorAll('select').forEach(select => {
            if (!select.disabled) select.selectedIndex = 0;
        });

        // Скрыть все группы периода
        periodGroups.forEach(group => group.style.display = 'none');

        // Обновить шаблоны
        filterTemplatesByType("");

        // Отправить форму
        form.submit();
    });

    // Инициализация при загрузке
    initializeFilters();

    // Пагинация (оставьте без изменений)
    const tableBody = document.querySelector('table tbody');
    const rows = Array.from(tableBody.querySelectorAll('tr')).filter(r => !r.classList.contains('no-data'));
    const rowsPerPage = 10;
    const paginationContainer = document.getElementById('pagination');
    let currentPage = 1;

    function renderTablePage(page) {
        const start = (page - 1) * rowsPerPage;
        const end = start + rowsPerPage;

        rows.forEach((row, index) => {
            row.style.display = index >= start && index < end ? '' : 'none';
        });
    }

    function renderPagination() {
        paginationContainer.innerHTML = '';
        const pageCount = Math.ceil(rows.length / rowsPerPage);

        if (pageCount <= 1) return;

        for (let i = 1; i <= pageCount; i++) {
            const btn = document.createElement('button');
            btn.textContent = i;
            btn.className = 'btn btn-sm mx-1 ' + (i === currentPage ? 'btn-primary' : 'btn-outline-primary');
            btn.addEventListener('click', () => {
                currentPage = i;
                renderTablePage(currentPage);
                renderPagination();
            });
            paginationContainer.appendChild(btn);
        }
    }

    if (rows.length > 0) {
        renderTablePage(currentPage);
        renderPagination();
    }
});