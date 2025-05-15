document.addEventListener('DOMContentLoaded', function () {
    function setupPagination(tableBodyId, controlsId, itemsPerPage) {
        const tableBody = document.getElementById(tableBodyId);
        if (!tableBody) {
            console.warn(`Table body with id "${tableBodyId}" not found.`);
            return;
        }
        const paginationControls = document.getElementById(controlsId);
        if (!paginationControls) {
            console.warn(`Pagination controls with id "${controlsId}" not found.`);
            return;
        }

        const rows = Array.from(tableBody.getElementsByTagName('tr'));
        const totalItems = rows.length;
        if (totalItems === 0) {
            paginationControls.innerHTML = '<p>Нет данных для отображения.</p>';
            return;
        }

        const totalPages = Math.ceil(totalItems / itemsPerPage);
        let currentPage = 1;

        function displayPage(page) {
            currentPage = page;
            const startIndex = (page - 1) * itemsPerPage;
            const endIndex = startIndex + itemsPerPage;

            rows.forEach((row, index) => {
                row.style.display = (index >= startIndex && index < endIndex) ? '' : 'none';
            });

            renderControls();
        }

        function renderControls() {
            paginationControls.innerHTML = ''; // Очистить предыдущие контролы

            if (totalPages <= 1) return; // Не нужны контролы если всего одна страница или меньше

            // Кнопка "Назад"
            const prevButton = document.createElement('button');
            prevButton.textContent = 'Назад';
            prevButton.disabled = currentPage === 1;
            prevButton.addEventListener('click', () => {
                if (currentPage > 1) {
                    displayPage(currentPage - 1);
                }
            });
            paginationControls.appendChild(prevButton);

            // Кнопки страниц
            // Для простоты можно отображать только несколько страниц вокруг текущей
            // или все, если их немного.
            // Здесь простой вариант: все страницы.
            for (let i = 1; i <= totalPages; i++) {
                const pageButton = document.createElement('button');
                pageButton.textContent = i;
                if (i === currentPage) {
                    pageButton.classList.add('active');
                    pageButton.disabled = true; // Уже на этой странице
                }
                pageButton.addEventListener('click', () => {
                    displayPage(i);
                });
                paginationControls.appendChild(pageButton);
            }

            // Кнопка "Вперед"
            const nextButton = document.createElement('button');
            nextButton.textContent = 'Вперед';
            nextButton.disabled = currentPage === totalPages;
            nextButton.addEventListener('click', () => {
                if (currentPage < totalPages) {
                    displayPage(currentPage + 1);
                }
            });
            paginationControls.appendChild(nextButton);
        }

        displayPage(1); // Показать первую страницу при инициализации
    }

    // Настройка пагинации для таблицы филиалов
    // Вы можете изменить '5' на желаемое количество элементов на странице
    if (document.getElementById('branchTableBody')) {
        setupPagination('branchTableBody', 'branchPaginationControls', 5);
    }

    // Настройка пагинации для таблицы пользователей
    // Вы можете изменить '5' на желаемое количество элементов на странице
    if (document.getElementById('userTableBody')) {
        setupPagination('userTableBody', 'userPaginationControls', 5);
    }
});