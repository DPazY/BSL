import http from 'k6/http';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';

// Загрузка датасета в общую память для всех VU
const bookNames = new SharedArray('bookNames', function () {
    return open('./books_name.csv')
        .split('\n')
        .map(s => s.replace(/"/g, '').trim())
        .filter(s => s.length > 0 && s !== 'Name' && s !== 'name');
});

// Настройка профиля нагрузки (Stages) для симуляции фрактальных всплесков
export const options = {
    stages: [
        { duration: '30s', target: 20 },  // Warm-up: Плавный разгон до 20 пользователей
        { duration: '10s', target: 150 }, // Burst: Резкий фрактальный всплеск (эффект толпы)
        { duration: '50s', target: 150 }, // Plateau: Удержание пиковой нагрузки
        { duration: '30s', target: 10 },  // Cool-down: Резкий спад
    ],
    thresholds: {
        http_req_duration: ['p(95)<200'], // 95% запросов должны выполняться быстрее 200мс
    },
};

export default function () {
    const baseUrl = 'http://localhost:14450/api/test/books/';
    const now = Date.now();

    // Эмуляция "вирального" тренда (Fractal Anomaly)
    // Допустим, время старта скрипта мы берем приблизительно, 
    // но в k6 лучше опираться на вероятностные всплески для конкретных индексов.

    let randomIndex;
    const rand = Math.random();

    // Генерируем "Внезапный тренд" на определенную книгу (например, индекс 105) 
    // с вероятностью 30%, чтобы IFS-предиктор смог уловить градиент и сделать префетч
    const isViralAnomaly = rand < 0.30;
    const isPopular = rand >= 0.30 && rand < 0.85; // 55% на обычный топ

    const topCount = 100;

    if (isViralAnomaly) {
        // Фокусная атака на 3 конкретные книги, которые изначально не в кэше
        const viralIndices = [105, 106, 107];
        randomIndex = viralIndices[Math.floor(Math.random() * viralIndices.length)];
    } else if (isPopular) {
        // Стандартное распределение Парето (горячий кэш)
        randomIndex = Math.floor(Math.random() * topCount);
    } else {
        // Длинный хвост (Long tail) - запросы к редким книгам (холодный кэш)
        randomIndex = topCount + Math.floor(Math.random() * (bookNames.length - topCount));
    }

    const bookName = bookNames[randomIndex];

    // Выполняем HTTP GET запрос
    const res = http.get(baseUrl + encodeURIComponent(bookName));

    // Небольшая задержка, чтобы имитировать реального пользователя
    sleep(Math.random() * 0.2 + 0.1);
}