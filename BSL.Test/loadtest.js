import http from 'k6/http';
import { sleep } from 'k6';
import { SharedArray } from 'k6/data';

const bookNames = new SharedArray('bookNames', function () {
    return open('./books_name.csv')
        .split('\n')
        .map(s => s.replace(/"/g, '').trim())
        .filter(s => s.length > 0 && s !== 'Name' && s !== 'name');
});

export const options = {
    vus: 10,
    duration: '120s',
};

export default function () {
    const baseUrl = 'http://localhost:14450/api/test/books/';

    let randomIndex;
    const isPopular = Math.random() < 0.8;

    const topCount = 100;

    if (isPopular) {
        randomIndex = Math.floor(Math.random() * topCount);
    } else {
        randomIndex = topCount + Math.floor(Math.random() * (bookNames.length - topCount));
    }

    const bookName = bookNames[randomIndex];
    http.get(baseUrl + encodeURIComponent(bookName));

    sleep(0.1);
}