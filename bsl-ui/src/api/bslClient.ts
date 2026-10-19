export interface Edition {
  name: string;
  author?: string;
  publisher?: string;
  placeOfPublication?: string;
  yearOfPublication: number;
  pagesCount: number;
}

export interface Book extends Edition {
  isbn?: string;
}

export interface Newspaper extends Edition {
  number?: number;
  date?: string;
  issn?: string;
}

export interface Patent extends Edition {
  registrationNumber?: string;
  applicationDate?: string;
  publicationDate?: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CacheBenchmarkResponse {
  totalRequests: number;
  ifsTotalMs: number;
  ifsP50Ms: number;
  ifsP95Ms: number;
  lruTotalMs: number;
  lruP50Ms: number;
  lruP95Ms: number;
  speedupFactor: number;
}

export interface RequestTelemetry {
  executionTimeMs: string;
  dataSource: string;
}

async function fetchWithTelemetry<T>(
  url: string,
  options: RequestInit = {}
): Promise<{ data: T; telemetry: RequestTelemetry }> {
  const start = performance.now();
  const response = await fetch(url, options);
  const clientElapsed = (performance.now() - start).toFixed(2);

  if (!response.ok) {
    const problem = await response.json().catch(() => ({ detail: response.statusText }));
    throw new Error(problem.detail || problem.title || `Ошибка HTTP ${response.status}`);
  }

  const data = (await response.json()) as T;
  const executionTimeMs = response.headers.get('X-Execution-Time-Ms') || clientElapsed;
  const dataSource = response.headers.get('X-Data-Source') || 'PostgreSQL';

  return { data, telemetry: { executionTimeMs, dataSource } };
}

export const bslClient = {
  getBooks: (params: {
    author?: string;
    publisherStartsWith?: string;
    orderByYear?: 'Asc' | 'Desc' | '';
    page?: number;
    pageSize?: number;
  }) => {
    const query = new URLSearchParams();
    if (params.author) query.set('Author', params.author);
    if (params.publisherStartsWith) query.set('PublisherStartsWith', params.publisherStartsWith);
    if (params.orderByYear) query.set('OrderByYear', params.orderByYear);
    query.set('Page', String(params.page ?? 1));
    query.set('PageSize', String(params.pageSize ?? 12));

    return fetchWithTelemetry<PagedResponse<Book>>(`/api/v1/catalog/books?${query.toString()}`);
  },

  getBookByName: (name: string) =>
    fetchWithTelemetry<Book>(`/api/v1/catalog/books/${encodeURIComponent(name)}`),

  getNewspapers: () => fetchWithTelemetry<Newspaper[]>('/api/v1/catalog/newspapers'),

  getPatents: () => fetchWithTelemetry<Patent[]>('/api/v1/catalog/patents'),

  warmupCache: (topCount = 15) =>
    fetchWithTelemetry<{ message: string; warmedCount: number; elapsedMs: number; keys: string[] }>(
      `/api/v1/cache/warmup?topCount=${topCount}`,
      { method: 'POST' }
    ),

  runBenchmark: (requestsCount = 300) =>
    fetchWithTelemetry<CacheBenchmarkResponse>(
      `/api/v1/cache/benchmark?requestsCount=${requestsCount}`,
      { method: 'POST' }
    ),

  uploadFile: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return fetchWithTelemetry<{ message: string; fileName: string; sizeBytes: number }>(
      '/api/v1/ingestion/upload',
      { method: 'POST', body: formData }
    );
  },
};