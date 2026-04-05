import http from 'k6/http';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';

// Загрузка словаря книг
const bookNames = new SharedArray('bookNames', function () {
    return open('./books_name.csv')
        .split('\n')
        .map(s => s.replace(/"/g, '').trim())
        .filter(s => s.length > 0 && s !== 'Name' && s !== 'name');
});

export const options = {
    stages: [
        { duration: '30s', target: 50 },  // Фаза 1: Фоновый шум (разогрев кэша "мусором")
        { duration: '10s', target: 200 }, // Фаза 2: Резкий всплеск (начало вирусности)
        { duration: '40s', target: 200 }, // Фаза 3: Удержание пика популярности
        { duration: '20s', target: 10 },  // Фаза 4: Спад интереса
    ],
    thresholds: {
        http_req_duration: ['p(95)<300'], // Допускаем легкую деградацию на старте
    },
};

export default function () {
    const baseUrl = 'http://localhost:14450/api/test/books/';
    const vuId = exec.vu.idInTest;

    // Получаем время в миллисекундах с начала теста. 
    // Это позволит синхронизировать всех VU для создания единого глобального спайка.
    const timeSinceStart = Date.now() - exec.scenario.startTime;

    let targetIndex;
    let sleepTime = 0.1;

    // Индекс книги, которая станет "вирусной".
    // Берем индекс из середины массива, чтобы он гарантированно 
    // не лежал в LRU-кэше от фазы прогрева.
    const viralBookIndex = Math.floor(bookNames.length / 2) + 42;

    // Триггер вирусности срабатывает на 30-й секунде (когда начинается вторая стадия)
    // и длится до 80-й секунды (конец третьей стадии).
    if (timeSinceStart > 30000 && timeSinceStart < 80000) {

        // Паттерн "Flash Crowd" (Формирование аттрактора)
        // 85% всего трафика системы внезапно направляется на одну книгу.
        if (Math.random() < 0.85) {
            targetIndex = viralBookIndex;
            // Уменьшаем sleep, так как вирусный трафик обычно более плотный
            sleepTime = 0.05;
        } else {
            // Оставшиеся 15% продолжают генерировать фоновый шум,
            // чтобы LRU продолжал пытаться вытеснять старые данные
            targetIndex = Math.floor(Math.random() * bookNames.length);
        }

    } else {

        // Паттерн "Фоновый шум" (Фазы 1 и 4)
        // Имитируем обычное поведение системы до и после инцидента.
        // Распределение 80/20: 80% запросов идут к 20% каталога (вымывание кэша).
        if (Math.random() < 0.2) {
            // "Горячий топ" (первые 50 книг)
            targetIndex = vuId % 50;
        } else {
            // "Длинный хвост" (случайное чтение всего каталога для вымывания LRU)
            const tailSize = bookNames.length - 50;
            targetIndex = 50 + Math.floor(Math.random() * tailSize);
        }
    }

    // Защита от выхода за пределы массива
    targetIndex = targetIndex % bookNames.length;
    const bookName = bookNames[targetIndex];

    const res = http.get(baseUrl + encodeURIComponent(bookName));

    sleep(sleepTime);
}