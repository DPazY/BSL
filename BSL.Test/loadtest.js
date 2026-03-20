import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    vus: 10,
    duration: '30s',

}
    export default function() {
        const baseUrl = 'http://localhost:14450/api/test/books/';
        const bookName = 'CLR via C#';

        http.get(baseUrl + encodeURIComponent(bookName));

        sleep(0.1); 
    }