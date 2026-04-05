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
        { duration: '30s', target: 20 },  // Разгон
        { duration: '10s', target: 150 }, // Фрактальный всплеск (эффект толпы)
        { duration: '50s', target: 150 }, // Удержание пиковой нагрузки
        { duration: '30s', target: 10 },  // Спад
    ],
    thresholds: {
        http_req_duration: ['p(95)<200'],
    },
};

export default function () {
    const baseUrl = 'http://localhost:14450/api/test/books/';

    // Вместо чистого Math.random() используем ID виртуального пользователя (VU) 
    // и номер его итерации, чтобы создать детерминированную траекторию (динамическую систему)
    const vuId = exec.vu.idInTest;
    const iter = exec.vu.iterationInScenario;

    let targetIndex;

    // Разбиваем поведение пользователя на циклы (например, по 10 итераций)
    // Это создает точки притяжения (аттракторы), которые алгоритм может изучить
    const patternStep = iter % 10;

    if (patternStep < 4) {
        // 1. Паттерн "Связанная последовательность" (Пространственная локальность)
        // Имитируем чтение серии книг (Том 1, Том 2, Том 3...).
        // LRU даст промах (miss) на каждом новом томе.
        // Математическая модель (IFS) уловит аффинное преобразование (X_n+1 = X_n + 1)
        // и успеет положить следующие тома в кэш заранее.
        const baseIndex = (vuId * 50) % (bookNames.length - 20);
        targetIndex = baseIndex + patternStep;

    } else if (patternStep >= 4 && patternStep < 7) {
        // 2. Классический "Горячий топ" (Парето)
        // Даем LRU немного поработать в комфортных условиях, 
        // имитируя обращения к главной странице или бестселлерам.
        targetIndex = vuId % 30;

    } else {
        // 3. Паттерн "Вымывание кэша" (Cache Churn / Scan Resistance)
        // Имитируем фоновый процесс или бота, который сканирует каталог.
        // Этот линейный проход быстро забьет ограниченный объем LRU-кэша, 
        // вытеснив оттуда "Горячий топ". Когда цикл вернется к шагу 4, LRU снова даст miss.
        // Интеллектуальный кэш должен распознать этот вектор как "не требующий долгого хранения".
        targetIndex = Math.floor(bookNames.length / 2) + (iter % 200);
    }

    const bookName = bookNames[targetIndex];

    const res = http.get(baseUrl + encodeURIComponent(bookName));

    // Варьируем задержку: последовательные переходы (чтение томов) происходят быстрее
    const sleepTime = (patternStep < 4) ? 0.05 : 0.2;
    sleep(sleepTime);
}