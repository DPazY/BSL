import http from 'k6/http';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';
import exec from 'k6/execution';

// Загрузка списка названий книг из CSV
const bookNames = new SharedArray('bookNames', function () {
    return open('./books_name.csv')
        .split('\n')
        .map(s => s.replace(/"/g, '').trim())
        .filter(s => s.length > 0 && s !== 'Name' && s !== 'name');
});

export const options = {
    stages: [
        { duration: '30s', target: 50 },   // Разогрев
        { duration: '30s', target: 200 },  // Эпоха 1 (30-60с)
        { duration: '30s', target: 200 },  // Эпоха 2 (60-90с)
        { duration: '30s', target: 200 },  // Эпоха 3 (90-120с)
        { duration: '20s', target: 10 },   // Остывание
    ],
    thresholds: {
        http_req_duration: ['p(95)<300'], // SLA
    },
};

// Генерация шума с длинным хвостом (распределение Парето / Ципфа)
function getBackgroundNoiseIndex(vuId, totalBooks) {
    // 10% запросов шума попадают в первые 50 книг (имитация локальной популярности)
    if (Math.random() < 0.1) {
        return vuId % 50;
    } else {
        // Остальные 90% — равномерно по всему остальному диапазону (длинный хвост)
        const tailSize = totalBooks - 50;
        return 50 + Math.floor(Math.random() * tailSize);
    }
}

export default function () {
    const host = __ENV.TARGET_URL || 'http://bsl-web:8080';
    const baseUrl = `${host}/books/`;

    const timeSinceStart = (Date.now() - exec.scenario.startTime) / 1000;
    const vuId = exec.vu.idInTest;

    // Аттракторы (популярные элементы) на разных интервалах времени
    const idx1 = Math.floor(bookNames.length * 0.3);  // 30%
    const idx2 = Math.floor(bookNames.length * 0.6);  // 60%
    const idx3 = Math.floor(bookNames.length * 0.9);  // 90%

    let targetIndex;
    let sleepTime = 0.1;

    // Фазы нагрузки (смена аттракторов)
    if (timeSinceStart > 30 && timeSinceStart <= 60) {
        // Эпоха 1: доминирует idx1
        if (Math.random() < 0.85) {
            targetIndex = idx1;
            sleepTime = 0.05; // высокая интенсивность
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId, bookNames.length);
            sleepTime = 0.2;
        }
    } else if (timeSinceStart > 60 && timeSinceStart <= 90) {
        // Эпоха 2: доминирует idx2
        if (Math.random() < 0.85) {
            targetIndex = idx2;
            sleepTime = 0.05;
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId, bookNames.length);
            sleepTime = 0.2;
        }
    } else if (timeSinceStart > 90 && timeSinceStart <= 120) {
        // Эпоха 3: доминирует idx3
        if (Math.random() < 0.85) {
            targetIndex = idx3;
            sleepTime = 0.05;
        } else {
            targetIndex = getBackgroundNoiseIndex(vuId, bookNames.length);
            sleepTime = 0.2;
        }
    } else {
        // Начальный разогрев и финальное остывание: только фоновый шум
        targetIndex = getBackgroundNoiseIndex(vuId, bookNames.length);
        sleepTime = 0.2;
    }

    // Защита от выхода за границы массива
    targetIndex = Math.max(0, Math.min(targetIndex, bookNames.length - 1));
    const bookName = bookNames[targetIndex];

    http.get(baseUrl + encodeURIComponent(bookName));
    sleep(sleepTime);
}