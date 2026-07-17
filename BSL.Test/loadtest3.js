import http from 'k6/http';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';

const bookNames = new SharedArray('bookNames', function () {
    return open('./books_name.csv')
        .split('\n')
        .map(s => s.replace(/"/g, '').trim())
        .filter(s => s.length > 0 && s !== 'Name' && s !== 'name');
});

export const options = {
    stages: [
        { duration: '30s', target: 50 },
        { duration: '3m', target: 200 },  // Даем 3 минуты, чтобы фракталы раскрылись
        { duration: '30s', target: 10 },
    ],
    thresholds: {
        http_req_duration: ['p(95)<300'],
    },
};

// Аппроксимация закона Парето/Ципфа для выбора книги.
function getZipfIndex(maxElements) {
    const rand = Math.random();
    // Чем выше степень, тем "острее" популярность топа
    const index = Math.floor(Math.pow(rand, 4) * maxElements);
    return index;
}

export default function () {
    const baseUrl = 'http://localhost:5155/books/';

    // Получаем текущее время в секундах
    const t = (Date.now() - exec.scenario.startTime) / 1000;

    // ---------------------------------------------------------
    // МАТЕМАТИКА ФРАКТАЛЬНОГО ТРАФИКА (Самоподобие)
    const wave1 = Math.sin(t * (Math.PI / 15));          // Базовый цикл 30 сек
    const wave2 = 0.5 * Math.sin(t * (Math.PI / 7.5));   // Всплески каждые 15 сек
    const wave3 = 0.25 * Math.sin(t * (Math.PI / 3.75)); // Микро-всплески каждые 7.5 сек

    // Нормализуем значение в диапазон [0, 1]
    const fractalIntensity = (wave1 + wave2 + wave3 + 1.75) / 3.5;
    // ---------------------------------------------------------

    let targetIndex;
    let sleepTime = 0.1;

    // В моменты фрактальных всплесков (интенсивность > 0.6) пользователи атакуют базу
    if (fractalIntensity > 0.6) {
        // ИСПРАВЛЕНИЕ 1: Бьем по популярному ядру из 300 книг
        targetIndex = getZipfIndex(300);
        sleepTime = 0.01;
    } else {
        // ИСПРАВЛЕНИЕ 2: Фоновый шум смещен за пределы топ-300
        const tailSize = bookNames.length - 300;
        // ИСПРАВЛЕНИЕ 3: Индекс начинается с 300
        targetIndex = 300 + Math.floor(Math.random() * tailSize);
        sleepTime = 0.2;
    }

    // Защита от выхода за пределы
    targetIndex = Math.max(0, Math.min(targetIndex, bookNames.length - 1));
    const bookName = bookNames[targetIndex];

    http.get(baseUrl + encodeURIComponent(bookName));
    sleep(sleepTime);
}