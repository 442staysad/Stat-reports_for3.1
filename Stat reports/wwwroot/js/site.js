document.addEventListener('DOMContentLoaded', function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub") // URL, который вы зарегистрировали в Program.cs
        .build();

    // Обработчик для получения обновленного счетчика уведомлений
    connection.on("ReceiveNotificationCount", function (unreadCount) {
        const notificationBadge = document.querySelector('#notificationBadge'); // ID вашего спана со счетчиком
        const notificationLink = document.querySelector('.nav-item.position-relative .nav-link'); // Селектор для ссылки

        if (notificationBadge) {
            if (unreadCount > 0) {
                notificationBadge.textContent = unreadCount;
                notificationBadge.style.display = ''; // Показать, если было скрыто
            } else {
                notificationBadge.style.display = 'none'; // Скрыть, если нет уведомлений
            }
        } else {
            // Если бейджа нет, но уведомления есть, создайте его
            if (unreadCount > 0 && notificationLink) {
                const newBadge = document.createElement('span');
                newBadge.id = 'notificationBadge'; // Добавьте ID
                newBadge.className = 'position-absolute top-10 start-100 translate-middle badge rounded-pill bg-danger';
                newBadge.textContent = unreadCount;
                notificationLink.appendChild(newBadge);
            }
        }
    });

    // Запуск соединения
    connection.start().then(function () {
        console.log("SignalR Connected.");
    }).catch(function (err) {
        return console.error(err.toString());
    });
});
