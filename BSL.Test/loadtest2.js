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
        { duration: '30s', target: 50 },  // Фаза 1: Разгон и жесткое вымывание кэша
        { duration: '30s', target: 200 }, // Фаза 2: Волна 1 (Аттрактор А)
        { duration: '30s', target: 200 }, // Фаза 3: Волна 2 (Аттрактор B)
        { duration: '30s', target: 200 }, // Фаза 4: Волна 3 (Аттрактор C)
        { duration: '20s', target: 10 },  // Фаза 5: Спад
    ],
    thresholds: {
        http_req_duration: ['p(95)<300'],
    },
};

export default function () {
    const baseUrl = 'http://localhost:14450/api/test/books/';
    const vuId = exec.vu.idInTest;
    const timeSinceStart = Date.now() - exec.scenario.startTime;

    let targetIndex;
    let sleepTime = 0.1;

    // Выбираем три разные книги, разбросанные по массиву, 
    // чтобы исключить случайное попадание в горячий топ
    const viralBook1 = Math.floor(bookNames.length * 0.3);
    const viralBook2 = Math.floor(bookNames.length * 0.6);
    const viralBook3 = Math.floor(bookNames.length * 0.9);

    // Логика каскадных аттракторов
    if (timeSinceStart > 30000 && timeSinceStart <= 60000) {
        // ВОЛНА 1
        if (Math.random() < 0.85) {
            targetIndex = viralBook1;
            sleepTime = 0.05;
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId);
        }
    } else if (timeSinceStart > 60000 && timeSinceStart <= 90000) {
        // ВОЛНА 2: Резкая смена тренда. LRU здесь получит второй удар.
        if (Math.random() < 0.85) {
            targetIndex = viralBook2;
            sleepTime = 0.05;
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId);
        }
    } else if (timeSinceStart > 90000 && timeSinceStart <= 120000) {
        // ВОЛНА 3: Третий тренд. База данных под LRU начинает задыхаться.
        if (Math.random() < 0.85) {
            targetIndex = viralBook3;
            sleepTime = 0.05;
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId);
        }
    } else {
        // Фоновый режим
        targetIndex = getBackgroundNoiseIndex(vuId);
    }

    // Защита от выхода за пределы массива
    targetIndex = targetIndex % bookNames.length;
    const bookName = bookNames[targetIndex];

    const res = http.get(baseUrl + encodeURIComponent(bookName));
    sleep(sleepTime);
}

// Выносим генерацию шума в отдельную функцию для чистоты кода
function getBackgroundNoiseIndex(vuId) {
    // Агрессивное вымывание (10% топ, 90% сканирование хвоста).
    // Это гарантирует, что LRU будет постоянно очищать память от полезных данных.
    if (Math.random() < 0.1) {
        return vuId % 50;
    } else {
        const tailSize = bookNames.length - 50;
        return 50 + Math.floor(Math.random() * tailSize);
    }
}