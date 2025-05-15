
document.addEventListener('DOMContentLoaded', function () => {
    // --- Элементы управления ---
    const typeTabs = document.querySelectorAll('#reportTypeTabs .nav-link');
    const branchTabs = document.querySelectorAll('#branchTabs .nav-link');
    const templateManagementModal = document.getElementById('templateManagementModal');
    const templateListContainer = document.getElementById('templateListContainer');
    const templateManagementError = document.getElementById('templateManagementError');


    // --- Пагинация ---
    // Настройки пагинации
    const rowsPerPage = 10; // количество строк на странице

    // Функция для применения фильтра по типу отчета и пагинации
    // в пределах *активного* таба филиала
    function filterAndPaginateActiveTab() {
        const activeTypeTab = document.querySelector('#reportTypeTabs .nav-link.active');
        const reportType = activeTypeTab ? activeTypeTab.getAttribute('data-report-type') : ''; // "" для "Все"

        // Находим активный таб филиала и его таблицу
        const activeBranchPane = document.querySelector('.tab-pane.show.active');
        if (!activeBranchPane) return; // Нет активного таба филиала

        const tableBody = activeBranchPane.querySelector('tbody');
        if (!tableBody) return; // Нет тела таблицы в этом табе

        const allRows = Array.from(tableBody.querySelectorAll('tr')); // Все строки в текущем табе
        let visibleRows = allRows.filter(row => {
            const rowType = row.getAttribute('data-report-type');
            // Строка видна, если тип не выбран ИЛИ тип строки совпадает с выбранным типом
            return !reportType || rowType === reportType;
        });

        // --- Применяем пагинацию к видимым строкам ---
        let currentPage = activeBranchPane.dataset.currentPage ? parseInt(activeBranchPane.dataset.currentPage) : 1;
        const totalVisibleItems = visibleRows.length;
        const totalPages = Math.ceil(totalVisibleItems / rowsPerPage);

        // Корректируем currentPage, если он стал больше общего количества страниц (например, после фильтрации)
        if (currentPage > totalPages && totalPages > 0) {
            currentPage = totalPages;
        } else if (totalPages === 0) {
            currentPage = 1; // Нет страниц, остаемся на 1 (или показываем "нет данных")
        }
        activeBranchPane.dataset.currentPage = currentPage; // Сохраняем текущую страницу для этого таба


        const startIndex = (currentPage - 1) * rowsPerPage;
        const endIndex = startIndex + rowsPerPage;

        // Скрываем все строки сначала
        allRows.forEach(row => row.style.display = 'none');

        // Показываем только строки из текущей страницы из *отфильтрованного* набора
        visibleRows.forEach((row, index) => {
            if (index >= startIndex && index < endIndex) {
                row.style.display = ''; // Показываем строку
            }
        });

        // --- Обновляем контролы пагинации для этого таба ---
        const paginationContainer = activeBranchPane.querySelector('.pagination');
        if (!paginationContainer) return;

        paginationContainer.innerHTML = ''; // Очищаем контролы

        if (totalPages <= 1) {
            // Если всего одна страница или нет видимых строк, скрываем контролы
            paginationContainer.style.display = 'none';
            // Опционально: показать сообщение "Нет отчетов" внутри таба, если visibleRows.length === 0
            if (totalVisibleItems === 0 && activeBranchPane.querySelector('.no-reports')) {
                activeBranchPane.querySelector('.no-reports').style.display = '';
            } else if (activeBranchPane.querySelector('.no-reports')) {
                activeBranchPane.querySelector('.no-reports').style.display = 'none';
            }
            return;
        } else {
            paginationContainer.style.display = 'flex'; // Показываем контролы если их больше одной страницы
            if (activeBranchPane.querySelector('.no-reports')) {
                activeBranchPane.querySelector('.no-reports').style.display = 'none'; // Скрываем "нет отчетов", если они появились
            }
        }


        // Кнопка "Предыдущая"
        const prevButton = document.createElement('button');
        prevButton.textContent = 'Назад';
        prevButton.className = 'btn btn-sm btn-outline-secondary prev-page';
        prevButton.disabled = currentPage === 1;
        prevButton.addEventListener('click', () => {
            if (currentPage > 1) {
                activeBranchPane.dataset.currentPage = currentPage - 1; // Сохраняем страницу перед перерисовкой
                filterAndPaginateActiveTab(); // Перерисовываем текущий активный таб
            }
        });
        paginationContainer.appendChild(prevButton);

        // Информация о текущей странице (опционально, но полезно)
        const pageInfo = document.createElement('span');
        pageInfo.className = 'page-info btn btn-sm btn-light mx-1'; // Добавляем классы для стиля
        pageInfo.textContent = `Страница ${currentPage} из ${totalPages}`;
        paginationContainer.appendChild(pageInfo);


        // Кнопка "Следующая"
        const nextButton = document.createElement('button');
        nextButton.textContent = 'Вперед';
        nextButton.className = 'btn btn-sm btn-outline-secondary next-page';
        nextButton.disabled = currentPage === totalPages;
        nextButton.addEventListener('click', () => {
            if (currentPage < totalPages) {
                activeBranchPane.dataset.currentPage = currentPage + 1; // Сохраняем страницу перед перерисовкой
                filterAndPaginateActiveTab(); // Перерисовываем текущий активный таб
            }
        });
        paginationContainer.appendChild(nextButton);
    }


    // --- Обработчики событий ---

    // Обработчики для табов типов отчетов (Все, Плановые, Бухгалтерские)
    typeTabs.forEach(tab => {
        tab.addEventListener('click', () => {
            typeTabs.forEach(t => t.classList.remove('active'));
            tab.classList.add('active');
            // При смене типа, сбрасываем пагинацию всех табов филиалов на 1-ю страницу
            document.querySelectorAll('.tab-pane').forEach(pane => {
                pane.dataset.currentPage = 1;
            });
            filterAndPaginateActiveTab(); // Применяем фильтр и пагинацию к активному табу
        });
    });

    // Обработчики для табов филиалов (событие показа таба)
    branchTabs.forEach(tab => {
        // Используем 'shown.bs.tab' событие Bootstrap
        tab.addEventListener('shown.bs.tab', () => {
            // При показе нового таба филиала, применяем текущий фильтр типа отчета и пагинацию к этому табу
            filterAndPaginateActiveTab();
        });
    });

    // --- Логика Модального Окна Управления Шаблонами ---

    if (templateManagementModal) {
        templateManagementModal.addEventListener('shown.bs.modal', async () => {
            console.log('Модальное окно управления шаблонами открыто. Начинаем загрузку...'); // Добавьте сюда
            // Очищаем предыдущее содержимое и показываем индикатор загрузки
            templateListContainer.innerHTML = '<p class="text-center">Загрузка шаблонов...</p><div class="d-flex justify-content-center"><div class="spinner-border text-primary" role="status"><span class="visually-hidden">Загрузка...</span></div></div>';
            templateManagementError.classList.add('d-none'); // Скрываем предыдущие ошибки

            try {
                console.log('URL для fetch:', templateManagementUrl); // Логируем URL
                // Выполняем AJAX GET запрос для получения списка шаблонов
                const response = await fetch(templateManagementUrl);

                console.log('Получен ответ от сервера. Статус:', response.status, 'OK:', response.ok); // Логируем статус ответа

                if (!response.ok) {
                    // Логируем полный объект ответа, если статус не OK
                    console.error("Ответ сервера не OK:", response);
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const templates = await response.json(); // Ожидаем список шаблонов в формате JSON
                console.log('Шаблоны успешно загружены:', templates); // Логируем полученные данные

                // Рендерим список шаблонов в модальном окне
                renderTemplatesInModal(templates);

            } catch (error) {
                console.error("Ошибка загрузки шаблонов:", error); // Логируем ошибку
                templateListContainer.innerHTML = ''; // Очищаем индикатор загрузки
                templateManagementError.textContent = `Не удалось загрузить список шаблонов: ${error.message}`;
                templateManagementError.classList.remove('d-none'); // Показываем ошибку
            }
        });
    }


    // Функция для рендеринга списка шаблонов в модальном окне
    function renderTemplatesInModal(templates) {
        templateListContainer.innerHTML = ''; // Очищаем контейнер перед добавлением новых элементов

        if (!templates || templates.length === 0) {
            templateListContainer.innerHTML = '<p class="text-center">Нет доступных шаблонов.</p>';
            return;
        }

        const listGroup = document.createElement('ul');
        listGroup.classList.add('list-group'); // Bootstrap класс списка

        templates.forEach(template => {
            const listItem = document.createElement('li');
            listItem.classList.add('list-group-item'); // Bootstrap класс элемента списка
            listItem.dataset.templateId = template.id; // Сохраняем ID шаблона в data-атрибуте

            // Текст элемента списка: Название (Тип)
            const templateText = document.createElement('span');
            templateText.textContent = `${template.name} (${template.type === 'Plan' ? 'Плановый' : 'Бухгалтерский'})`;
            listItem.appendChild(templateText);

            // --- Кнопка УДАЛЕНИЯ ШАБЛОНА ---
            const deleteButtonContainer = document.createElement('div'); // Контейнер для кнопки

            // Определяем, имеет ли текущий пользователь право удалить этот конкретный шаблон
            // userPermissions определен в секции Scripts Razor View
            const canDeleteThisTemplate = userPermissions.canDeleteAny ||
                (userPermissions.canDeletePlan && template.type === 'Plan') ||
                (userPermissions.canDeleteAccountant && template.type === 'Accountant');

            if (canDeleteThisTemplate) {
                const deleteButtonForm = document.createElement('form');
                deleteButtonForm.classList.add('d-inline-block');
                // Важно: используем POST метод для удаления
                deleteButtonForm.method = 'post';
                deleteButtonForm.action = `${deleteTemplateUrl}/${template.id}`; // URL + ID шаблона

                // Добавляем токен антиподделки (нужно получить его из Razor View и передать в JS)
                // Более надежно - получить токен со страницы и добавить в форму
                const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]');
                if (antiForgeryToken) {
                    const tokenInput = document.createElement('input');
                    tokenInput.type = 'hidden';
                    tokenInput.name = '__RequestVerificationToken';
                    tokenInput.value = antiForgeryToken.value;
                    deleteButtonForm.appendChild(tokenInput);
                }


                const deleteButton = document.createElement('button');
                deleteButton.type = 'submit'; // Кнопка отправляет форму
                deleteButton.classList.add('btn', 'btn-sm', 'btn-danger'); // Bootstrap классы
                deleteButton.textContent = 'Удалить';
                deleteButton.title = 'Удалить шаблон';

                // Добавляем подтверждение перед отправкой
                deleteButton.onclick = function () {
                    return confirm('Вы уверены, что хотите удалить этот шаблон отчета? Это также может повлиять на связанные сроки сдачи.');
                };

                deleteButtonForm.appendChild(deleteButton);
                deleteButtonContainer.appendChild(deleteButtonForm);
                listItem.appendChild(deleteButtonContainer);
            }

            listGroup.appendChild(listItem);
        });

        templateListContainer.appendChild(listGroup);
    }


    // --- Инициализация при загрузке ---
    // Инициализация: применяем фильтр "Все отчеты" и пагинацию к первому активному табу филиала
    // Делаем это после того, как Bootstrap активировал первый таб (`shown.bs.tab` не сработает для первого таба при загрузке)
    const firstBranchPane = document.querySelector('.tab-pane.show.active');
    if (firstBranchPane) {
        firstBranchPane.dataset.currentPage = 1; // Устанавливаем начальную страницу
        filterAndPaginateActiveTab();
    } else {
        // Если по какой-то причине нет активных табов (например, visibleBranches пуст)
        console.log("Нет активных табов филиалов для инициализации пагинации.");
    }


    document.querySelectorAll('.tab-pane .pagination').forEach(container => {
        container.addEventListener('click', (event) => {
            const target = event.target;
            if (target.classList.contains('prev-page') && !target.disabled) {
                const activeBranchPane = target.closest('.tab-pane');
                let currentPage = parseInt(activeBranchPane.dataset.currentPage);
                activeBranchPane.dataset.currentPage = currentPage - 1;
                filterAndPaginateActiveTab();
            } else if (target.classList.contains('next-page') && !target.disabled) {
                const activeBranchPane = target.closest('.tab-pane');
                let currentPage = parseInt(activeBranchPane.dataset.currentPage);
                activeBranchPane.dataset.currentPage = currentPage + 1;
                filterAndPaginateActiveTab();
            }
        });
    });

});