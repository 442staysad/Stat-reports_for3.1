document.addEventListener('DOMContentLoaded', function () {
    const reportId = @Model.ReportId;
    const excelTabNav = document.getElementById('excelTabNav');
    const excelSheetContentArea = document.getElementById('excelSheetContentArea');
    const loadingMessage = document.getElementById('loadingMessage');

    fetch(`@Url.Action("DownloadExcelForView", "ReportMvc")?reportId=${reportId}`)
        .then(response => {
            if (!response.ok) {
                throw new Error(`Ошибка сети: ${response.statusText}`);
            }
            return response.arrayBuffer();
        })
        .then(data => {
            if (loadingMessage) loadingMessage.style.display = 'none';
            excelSheetContentArea.innerHTML = ''; // Очищаем предыдущее содержимое (например, сообщение о загрузке)

            const wb = XLSX.read(new Uint8Array(data), { type: 'array', cellStyles: true });

            if (!wb.SheetNames || wb.SheetNames.length === 0) {
                excelSheetContentArea.innerHTML = '<p class="p-3 text-danger">Файл Excel не содержит листов или пуст.</p>';
                return;
            }

            // Сохраняем HTML для каждого листа
            const sheetHtmlContents = {};

            wb.SheetNames.forEach((name, index) => {
                // Создаем вкладку
                const tabItem = document.createElement('li');
                tabItem.classList.add('nav-item');

                const tabLink = document.createElement('a');
                tabLink.classList.add('nav-link');
                if (index === 0) {
                    tabLink.classList.add('active'); // Первая вкладка активна по умолчанию
                }
                // tabLink.href = `#`; // Не используем href для предотвращения перехода
                tabLink.textContent = name;
                tabLink.dataset.sheetName = name; // Сохраняем имя листа для идентификации

                tabItem.appendChild(tabLink);
                excelTabNav.appendChild(tabItem);

                // Генерируем и сохраняем HTML для листа
                const sheet = wb.Sheets[name];
                const sheetContentDiv = document.createElement('div');
                sheetContentDiv.classList.add('sheet-content');
                // Не добавляем 'active' класс здесь, сделаем это после создания всех вкладок

                // Обертка для прокрутки
                const scrollWrapper = document.createElement('div');
                scrollWrapper.classList.add('scrollable-wrapper');

                const htmlStr = XLSX.utils.sheet_to_html(sheet, {
                    editable: false,
                    header: "", // Пустой заголовок, если не нужен стандартный
                });
                scrollWrapper.innerHTML = htmlStr;
                sheetContentDiv.appendChild(scrollWrapper);
                sheetHtmlContents[name] = sheetContentDiv; // Сохраняем div с содержимым

                // Если это первый лист, отображаем его содержимое
                if (index === 0) {
                    sheetContentDiv.classList.add('active');
                    excelSheetContentArea.appendChild(sheetContentDiv);
                }
            });

            // Обработчик кликов по вкладкам
            excelTabNav.addEventListener('click', function (e) {
                if (e.target && e.target.classList.contains('nav-link')) {
                    e.preventDefault();
                    const clickedSheetName = e.target.dataset.sheetName;

                    // Убираем класс 'active' у всех вкладок и скрываем все содержимое
                    excelTabNav.querySelectorAll('.nav-link').forEach(link => link.classList.remove('active'));
                    excelSheetContentArea.innerHTML = ''; // Очищаем область содержимого

                    // Активируем кликнутую вкладку
                    e.target.classList.add('active');

                    // Отображаем соответствующее содержимое листа
                    if (sheetHtmlContents[clickedSheetName]) {
                        excelSheetContentArea.appendChild(sheetHtmlContents[clickedSheetName]);
                        // Убедимся, что контейнер активного листа видим (хотя он уже должен быть, если мы его только что добавили)
                        sheetHtmlContents[clickedSheetName].classList.add('active');
                    }
                }
            });

        })
        .catch(err => {
            if (loadingMessage) loadingMessage.style.display = 'none';
            excelSheetContentArea.innerHTML = `<p class="p-3 text-danger">Не удалось загрузить или обработать отчёт: ${err.message}</p>`;
            console.error("Ошибка при загрузке/обработке Excel файла:", err);
        });
});