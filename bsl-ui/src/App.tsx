import React, { useEffect, useState } from 'react';
import {
  Activity,
  BookOpen,
  Clock,
  Database,
  Flame,
  Newspaper as NewspaperIcon,
  FileBadge,
  Search,
  UploadCloud,
  Zap,
  CheckCircle2,
  AlertCircle,
  Sparkles,
  Server,
  Layers,
  ArrowRight,
  RefreshCw,
  Terminal,
  SlidersHorizontal,
  FileSpreadsheet,
} from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  Legend,
  ResponsiveContainer,
  CartesianGrid,
} from 'recharts';
import { bslClient } from './api/bslClient';
import type {
  Book,
  Newspaper,
  Patent,
  CacheBenchmarkResponse,
  RequestTelemetry,
} from './api/bslClient';

interface TraceEntry {
  id: string;
  time: string;
  action: string;
  source: string;
  latencyMs: string;
}

const DEMO_BOOKS: Book[] = [
  {
    name: 'Проектирование высоконагруженных систем',
    author: 'Мартин Клеппман',
    publisher: 'Питер',
    placeOfPublication: 'СПб',
    yearOfPublication: 2024,
    pagesCount: 640,
    isbn: '978-5-4461-0512-0',
  },
  {
    name: 'CLR via C#. Программирование на платформе .NET',
    author: 'Джеффри Рихтер',
    publisher: 'Питер',
    placeOfPublication: 'СПб',
    yearOfPublication: 2023,
    pagesCount: 896,
    isbn: '978-5-496-00433-6',
  },
  {
    name: 'Фракталы и хаос в динамических системах',
    author: 'Ричард Кроновер',
    publisher: 'Техносфера',
    placeOfPublication: 'Москва',
    yearOfPublication: 2021,
    pagesCount: 488,
    isbn: '978-5-94836-102-4',
  },
  {
    name: 'Внутреннее устройство PostgreSQL 16',
    author: 'Егор Рогов',
    publisher: 'ДМК Пресс',
    placeOfPublication: 'Москва',
    yearOfPublication: 2024,
    pagesCount: 660,
    isbn: '978-5-93700-145-0',
  },
  {
    name: 'Предметно-ориентированное проектирование (DDD)',
    author: 'Эрик Эванс',
    publisher: 'Вильямс',
    placeOfPublication: 'Москва',
    yearOfPublication: 2022,
    pagesCount: 448,
    isbn: '978-5-8459-1689-1',
  },
  {
    name: 'Конкурентность в C#: асинхронное и параллельное программирование',
    author: 'Стивен Клири',
    publisher: 'Питер',
    placeOfPublication: 'СПб',
    yearOfPublication: 2023,
    pagesCount: 304,
    isbn: '978-5-4461-1479-5',
  },
];

const DEMO_BENCHMARK: CacheBenchmarkResponse = {
  totalRequests: 300,
  ifsTotalMs: 42.8,
  ifsP50Ms: 0.11,
  ifsP95Ms: 0.34,
  lruTotalMs: 186.4,
  lruP50Ms: 0.48,
  lruP95Ms: 2.15,
  speedupFactor: 4.36,
};

export default function App() {
  const [activeTab, setActiveTab] = useState<'catalog' | 'benchmark' | 'ingestion'>('catalog');
  const [entityType, setEntityType] = useState<'books' | 'newspapers' | 'patents'>('books');

  const [books, setBooks] = useState<Book[]>([]);
  const [newspapers, setNewspapers] = useState<Newspaper[]>([]);
  const [patents, setPatents] = useState<Patent[]>([]);
  const [authorFilter, setAuthorFilter] = useState('');
  const [publisherFilter, setPublisherFilter] = useState('');
  const [orderByYear, setOrderByYear] = useState<'Asc' | 'Desc' | ''>('');

  const [isLoading, setIsLoading] = useState(false);
  const [demoMode, setDemoMode] = useState(false);
  const [telemetry, setTelemetry] = useState<RequestTelemetry | null>(null);
  const [traces, setTraces] = useState<TraceEntry[]>([]);
  const [inspectedBook, setInspectedBook] = useState<{ book: Book; telemetry: RequestTelemetry } | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [requestsCount, setRequestsCount] = useState<number>(300);
  const [benchmark, setBenchmark] = useState<CacheBenchmarkResponse | null>(null);
  const [isRunningBenchmark, setIsRunningBenchmark] = useState(false);
  const [warmupStatus, setWarmupStatus] = useState<string | null>(null);

  const [uploadResult, setUploadResult] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const pushTrace = (action: string, t: RequestTelemetry) => {
    setTelemetry(t);
    setTraces((prev) => [
      {
        id: Math.random().toString(36).slice(2, 8),
        time: new Date().toLocaleTimeString(),
        action,
        source: t.dataSource,
        latencyMs: t.executionTimeMs,
      },
      ...prev.slice(0, 5),
    ]);
  };

  const loadCatalog = async () => {
    if (demoMode) {
      setBooks(
        DEMO_BOOKS.filter(
          (b) =>
            (!authorFilter || b.author?.toLowerCase().includes(authorFilter.toLowerCase())) &&
            (!publisherFilter || b.publisher?.toLowerCase().startsWith(publisherFilter.toLowerCase()))
        )
      );
      pushTrace('GET /api/v1/catalog/books (Demo)', {
        executionTimeMs: '0.18',
        dataSource: 'IFS-Cache',
      });
      return;
    }

    try {
      setIsLoading(true);
      setError(null);
      if (entityType === 'books') {
        const res = await bslClient.getBooks({
          author: authorFilter,
          publisherStartsWith: publisherFilter,
          orderByYear,
        });
        const rawData = res.data as any;
        const items: Book[] = Array.isArray(rawData)
          ? rawData
          : rawData?.items ?? rawData?.Items ?? [];
        setBooks(items);
        pushTrace('GET /api/v1/catalog/books', res.telemetry);
      } else if (entityType === 'newspapers') {
        const res = await bslClient.getNewspapers();
        setNewspapers(Array.isArray(res.data) ? res.data : []);
        pushTrace('GET /api/v1/catalog/newspapers', res.telemetry);
      } else {
        const res = await bslClient.getPatents();
        setPatents(Array.isArray(res.data) ? res.data : []);
        pushTrace('GET /api/v1/catalog/patents', res.telemetry);
      }
    } catch (err: any) {
      setError(err.message || 'Не удалось получить данные от сервера');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadCatalog();
  }, [entityType, orderByYear, demoMode]);

  const handleInspectBook = async (book: Book) => {
    if (demoMode) {
      const demoTelemetry = { executionTimeMs: '0.09', dataSource: 'IFS-Cache' };
      setInspectedBook({ book, telemetry: demoTelemetry });
      pushTrace(`GET /books/${book.name.slice(0, 18)}...`, demoTelemetry);
      return;
    }

    try {
      setError(null);
      const res = await bslClient.getBookByName(book.name);
      setInspectedBook({ book: res.data, telemetry: res.telemetry });
      pushTrace(`GET /books/${book.name.slice(0, 18)}...`, res.telemetry);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleRunBenchmark = async () => {
    if (demoMode) {
      setIsRunningBenchmark(true);
      setTimeout(() => {
        const factor = requestsCount / 300;
        setBenchmark({
          ...DEMO_BENCHMARK,
          totalRequests: requestsCount,
          ifsTotalMs: Number((DEMO_BENCHMARK.ifsTotalMs * factor).toFixed(1)),
          lruTotalMs: Number((DEMO_BENCHMARK.lruTotalMs * factor).toFixed(1)),
        });
        pushTrace(`POST /cache/benchmark (${requestsCount})`, {
          executionTimeMs: (DEMO_BENCHMARK.ifsTotalMs * factor).toFixed(1),
          dataSource: 'IFS-Cache',
        });
        setIsRunningBenchmark(false);
      }, 450);
      return;
    }

    try {
      setIsRunningBenchmark(true);
      setError(null);
      const res = await bslClient.runBenchmark(requestsCount);
      setBenchmark(res.data);
      pushTrace(`POST /cache/benchmark (${requestsCount})`, res.telemetry);
    } catch (err: any) {
      setError(
        `${err.message}. Если база пуста, включите «Демо-режим» в шапке или загрузите книги во вкладке FileWatcher.`
      );
    } finally {
      setIsRunningBenchmark(false);
    }
  };

  const handleWarmup = async () => {
    if (demoMode) {
      setWarmupStatus('Аттрактор IFS рассчитан: прогрето 15 горячих ключей за 1.42 мс');
      pushTrace('POST /cache/warmup', { executionTimeMs: '1.42', dataSource: 'IFS-Cache' });
      return;
    }

    try {
      setError(null);
      const res = await bslClient.warmupCache(15);
      setWarmupStatus(
        `${res.data.message}: прогрето ${res.data.warmedCount} шт. за ${res.data.elapsedMs} мс`
      );
      pushTrace('POST /cache/warmup', res.telemetry);
    } catch (err: any) {
      setError(err.message);
    }
  };

  const handleFileUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      setIsUploading(true);
      setError(null);
      const res = await bslClient.uploadFile(file);
      setUploadResult(`${res.data.message} (${res.data.fileName}, ${res.data.sizeBytes} байт)`);
      pushTrace(`POST /ingestion/upload (${file.name})`, res.telemetry);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setIsUploading(false);
    }
  };

  const handleGenerateSampleCsv = async () => {
    const csvContent = [
      'Name,Author,Publisher,PlaceOfPublication,YearOfPublication,PagesCount,ISBN',
      '"Проектирование высоконагруженных систем","Мартин Клеппман","Питер","СПб",2024,640,"978-5-4461-0512-0"',
      '"CLR via C#","Джеффри Рихтер","Питер","СПб",2023,896,"978-5-496-00433-6"',
      '"Внутреннее устройство PostgreSQL","Егор Рогов","ДМК Пресс","Москва",2024,660,"978-5-93700-145-0"',
      '"Фракталы и хаос в динамических системах","Ричард Кроновер","Техносфера","Москва",2021,488,"978-5-94836-102-4"',
    ].join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const sampleFile = new File([blob], 'books_ready_for_postgres.csv', { type: 'text/csv' });

    if (demoMode) {
      setUploadResult(
        'Тестовый пакет books_ready_for_postgres.csv (4 издания) отправлен в конвейер (Демо-режим)'
      );
      return;
    }

    try {
      setIsUploading(true);
      setError(null);
      const res = await bslClient.uploadFile(sampleFile);
      setUploadResult(`${res.data.message} (${res.data.fileName}, ${res.data.sizeBytes} байт)`);
      pushTrace('POST /ingestion/upload (sample CSV)', res.telemetry);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setIsUploading(false);
    }
  };

  const latencyChartData = benchmark
    ? [
        {
          metric: 'Медиана p50 (мс)',
          'IFS Predictive Cache': benchmark.ifsP50Ms,
          'LRU Cache': benchmark.lruP50Ms,
        },
        {
          metric: 'Хвостовая p95 (мс)',
          'IFS Predictive Cache': benchmark.ifsP95Ms,
          'LRU Cache': benchmark.lruP95Ms,
        },
      ]
    : [];

  const totalTimeChartData = benchmark
    ? [
        {
          metric: `Суммарное время (${benchmark.totalRequests} запр., мс)`,
          'IFS Predictive Cache': benchmark.ifsTotalMs,
          'LRU Cache': benchmark.lruTotalMs,
        },
      ]
    : [];

  const currentItemsCount =
    entityType === 'books'
      ? books.length
      : entityType === 'newspapers'
      ? newspapers.length
      : patents.length;

  return (
    <div className="min-h-screen bg-[#070b14] text-slate-100 font-sans selection:bg-indigo-500/30">
      {/* Фоновое свечение */}
      <div className="fixed inset-0 pointer-events-none overflow-hidden">
        <div className="absolute -top-40 left-1/4 w-96 h-96 bg-indigo-600/10 rounded-full blur-3xl" />
        <div className="absolute top-20 right-1/4 w-80 h-80 bg-emerald-500/5 rounded-full blur-3xl" />
      </div>

      <header className="border-b border-slate-800/80 bg-slate-900/75 backdrop-blur-md sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-6 py-4 flex flex-wrap items-center justify-between gap-4">
          <div className="flex items-center gap-3.5">
            <div className="p-2.5 rounded-xl bg-gradient-to-br from-indigo-500/20 to-emerald-500/10 border border-indigo-500/30 text-indigo-400 shadow-lg shadow-indigo-500/10">
              <Zap className="w-6 h-6" />
            </div>
            <div>
              <div className="flex items-center gap-2.5">
                <h1 className="text-lg font-bold tracking-tight text-white">BSL Studio</h1>
                <span className="text-[11px] font-medium px-2.5 py-0.5 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/25">
                  .NET 10 + IFS Predictive Cache
                </span>
              </div>
              <p className="text-xs text-slate-400 mt-0.5">
                Control Plane управления каталогом изданий, предиктивным кэшированием и FileWatcher
              </p>
            </div>
          </div>

          <div className="flex items-center gap-3">
            {/* Переключатель Демо-режима */}
            <button
              onClick={() => {
                const next = !demoMode;
                setDemoMode(next);
                if (next && !benchmark) setBenchmark(DEMO_BENCHMARK);
              }}
              className={`px-3 py-1.5 rounded-lg text-xs font-medium border transition flex items-center gap-1.5 cursor-pointer ${
                demoMode
                  ? 'bg-amber-500/15 text-amber-300 border-amber-500/40'
                  : 'bg-slate-900 text-slate-400 border-slate-800 hover:text-slate-200'
              }`}
              title="Включить демонстрационные данные, если PostgreSQL ещё пуст"
            >
              <Sparkles className="w-3.5 h-3.5" />
              {demoMode ? 'Демо-данные: ВКЛ' : 'Демо-данные'}
            </button>

            {telemetry && (
              <div className="flex items-center gap-3 text-xs bg-slate-950/90 px-3.5 py-2 rounded-xl border border-slate-800">
                <span
                  className={`px-2 py-0.5 rounded-md font-semibold ${
                    telemetry.dataSource === 'IFS-Cache'
                      ? 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30'
                      : 'bg-amber-500/15 text-amber-400 border border-amber-500/30'
                  }`}
                >
                  {telemetry.dataSource}
                </span>
                <span className="flex items-center gap-1 text-slate-200 font-mono">
                  <Clock className="w-3.5 h-3.5 text-indigo-400" />
                  {telemetry.executionTimeMs} ms
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Навигационные вкладки */}
        <div className="max-w-7xl mx-auto px-6 flex gap-6 text-sm">
          <button
            onClick={() => setActiveTab('catalog')}
            className={`pb-3 border-b-2 font-medium flex items-center gap-2 transition cursor-pointer ${
              activeTab === 'catalog'
                ? 'border-indigo-500 text-indigo-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <BookOpen className="w-4 h-4" />
            Каталог изданий
          </button>
          <button
            onClick={() => setActiveTab('benchmark')}
            className={`pb-3 border-b-2 font-medium flex items-center gap-2 transition cursor-pointer ${
              activeTab === 'benchmark'
                ? 'border-indigo-500 text-indigo-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <Activity className="w-4 h-4" />
            Бенчмарк кэша (IFS vs LRU)
          </button>
          <button
            onClick={() => setActiveTab('ingestion')}
            className={`pb-3 border-b-2 font-medium flex items-center gap-2 transition cursor-pointer ${
              activeTab === 'ingestion'
                ? 'border-indigo-500 text-indigo-400'
                : 'border-transparent text-slate-400 hover:text-slate-200'
            }`}
          >
            <UploadCloud className="w-4 h-4" />
            Потоковая загрузка (FileWatcher)
          </button>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-6 py-6 space-y-6 relative z-10">
        {/* Верхняя строка телеметрии (KPI Strip) */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800/80 flex items-center justify-between">
            <div>
              <div className="text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                Источник ответа
              </div>
              <div className="text-base font-bold mt-1 flex items-center gap-2">
                {telemetry ? telemetry.dataSource : 'Ожидание запроса'}
              </div>
            </div>
            <div className="p-2.5 rounded-lg bg-emerald-500/10 text-emerald-400">
              <Layers className="w-5 h-5" />
            </div>
          </div>

          <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800/80 flex items-center justify-between">
            <div>
              <div className="text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                Время выполнения
              </div>
              <div className="text-base font-bold font-mono mt-1 text-indigo-400">
                {telemetry ? `${telemetry.executionTimeMs} ms` : '—'}
              </div>
            </div>
            <div className="p-2.5 rounded-lg bg-indigo-500/10 text-indigo-400">
              <Clock className="w-5 h-5" />
            </div>
          </div>

          <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800/80 flex items-center justify-between">
            <div>
              <div className="text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                Записей в выборке
              </div>
              <div className="text-base font-bold font-mono mt-1">{currentItemsCount}</div>
            </div>
            <div className="p-2.5 rounded-lg bg-sky-500/10 text-sky-400">
              <Database className="w-5 h-5" />
            </div>
          </div>

          <div className="p-4 rounded-xl bg-slate-900/80 border border-slate-800/80 flex items-center justify-between">
            <div>
              <div className="text-[11px] uppercase tracking-wider text-slate-400 font-semibold">
                Алгоритм вытеснения
              </div>
              <div className="text-base font-bold mt-1 text-emerald-400">IFS + KKT Eviction</div>
            </div>
            <div className="p-2.5 rounded-lg bg-purple-500/10 text-purple-400">
              <Server className="w-5 h-5" />
            </div>
          </div>
        </div>

        {error && (
          <div className="p-4 rounded-xl bg-red-500/10 border border-red-500/30 text-red-300 text-sm flex items-center justify-between gap-3">
            <div className="flex items-center gap-3">
              <AlertCircle className="w-5 h-5 shrink-0 text-red-400" />
              <span>{error}</span>
            </div>
            <button
              onClick={() => {
                setDemoMode(true);
                setError(null);
              }}
              className="px-3 py-1.5 rounded-lg bg-red-500/20 hover:bg-red-500/30 text-xs font-semibold shrink-0 cursor-pointer"
            >
              Включить демо-данные
            </button>
          </div>
        )}

        {/* ВКЛАДКА 1: КАТАЛОГ */}
        {activeTab === 'catalog' && (
          <div className="space-y-6">
            {/* Панель фильтрации в 2 аккуратных ряда */}
            <div className="bg-slate-900/90 border border-slate-800 p-4 rounded-xl space-y-4">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <div className="flex gap-2">
                  <button
                    onClick={() => setEntityType('books')}
                    className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer ${
                      entityType === 'books'
                        ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                        : 'bg-slate-800/80 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    <BookOpen className="w-3.5 h-3.5" /> Книги
                  </button>
                  <button
                    onClick={() => setEntityType('newspapers')}
                    className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer ${
                      entityType === 'newspapers'
                        ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                        : 'bg-slate-800/80 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    <NewspaperIcon className="w-3.5 h-3.5" /> Газеты
                  </button>
                  <button
                    onClick={() => setEntityType('patents')}
                    className={`px-3.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition cursor-pointer ${
                      entityType === 'patents'
                        ? 'bg-indigo-600 text-white shadow-md shadow-indigo-600/20'
                        : 'bg-slate-800/80 text-slate-300 hover:bg-slate-800'
                    }`}
                  >
                    <FileBadge className="w-3.5 h-3.5" /> Патенты
                  </button>
                </div>

                <button
                  onClick={loadCatalog}
                  className="px-3 py-1.5 rounded-lg bg-slate-800 hover:bg-slate-700 text-slate-300 text-xs font-medium flex items-center gap-1.5 cursor-pointer"
                >
                  <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
                  Обновить
                </button>
              </div>

              {entityType === 'books' && (
                <div className="grid grid-cols-1 sm:grid-cols-4 gap-3 pt-3 border-t border-slate-800/80">
                  <input
                    type="text"
                    placeholder="Фильтр по автору..."
                    value={authorFilter}
                    onChange={(e) => setAuthorFilter(e.target.value)}
                    className="bg-slate-950 border border-slate-800 focus:border-indigo-500 rounded-lg px-3 py-2 text-xs focus:outline-none"
                  />
                  <input
                    type="text"
                    placeholder="Издательство начинается с..."
                    value={publisherFilter}
                    onChange={(e) => setPublisherFilter(e.target.value)}
                    className="bg-slate-950 border border-slate-800 focus:border-indigo-500 rounded-lg px-3 py-2 text-xs focus:outline-none"
                  />
                  <select
                    value={orderByYear}
                    onChange={(e) => setOrderByYear(e.target.value as any)}
                    className="bg-slate-950 border border-slate-800 focus:border-indigo-500 rounded-lg px-3 py-2 text-xs focus:outline-none"
                  >
                    <option value="">Без сортировки по году</option>
                    <option value="Asc">Год: по возрастанию (Asc)</option>
                    <option value="Desc">Год: по убыванию (Desc)</option>
                  </select>
                  <button
                    onClick={loadCatalog}
                    className="px-4 py-2 bg-indigo-600 hover:bg-indigo-500 text-xs font-semibold rounded-lg flex items-center justify-center gap-1.5 transition cursor-pointer"
                  >
                    <Search className="w-3.5 h-3.5" /> Применить фильтр
                  </button>
                </div>
              )}
            </div>

            {inspectedBook && (
              <div className="p-5 rounded-xl bg-indigo-950/30 border border-indigo-500/40 flex flex-wrap items-center justify-between gap-4">
                <div>
                  <span className="text-[11px] text-indigo-400 font-semibold uppercase tracking-wider">
                    Точечная проверка ключа GetByName (IFS-аттрактор)
                  </span>
                  <h3 className="text-lg font-bold mt-1">{inspectedBook.book.name}</h3>
                  <p className="text-xs text-slate-400 mt-1">
                    Автор: {inspectedBook.book.author || '—'} • Издательство:{' '}
                    {inspectedBook.book.publisher || '—'} • Год: {inspectedBook.book.yearOfPublication} •
                    Стр.: {inspectedBook.book.pagesCount}
                  </p>
                </div>
                <div className="text-right font-mono bg-slate-950/80 px-4 py-2.5 rounded-xl border border-slate-800">
                  <div className="text-xl font-bold text-emerald-400">
                    {inspectedBook.telemetry.executionTimeMs} ms
                  </div>
                  <div className="text-xs text-slate-400">{inspectedBook.telemetry.dataSource}</div>
                </div>
              </div>
            )}

            {/* Состояние пустого каталога */}
            {!isLoading && currentItemsCount === 0 && (
              <div className="p-12 rounded-2xl bg-slate-900/50 border border-dashed border-slate-800 text-center max-w-xl mx-auto my-4">
                <Database className="w-10 h-10 text-slate-600 mx-auto mb-3" />
                <h3 className="text-base font-bold text-slate-200">
                  В разделе пока нет загруженных записей
                </h3>
                <p className="text-xs text-slate-400 mt-1.5 leading-relaxed">
                  База данных PostgreSQL сейчас пуста или фильтр не нашёл совпадений. Вы можете отправить
                  тестовый CSV-пакет в конвейер FileWatcher либо включить демонстрационный набор.
                </p>
                <div className="flex flex-wrap justify-center gap-3 mt-5">
                  <button
                    onClick={() => setDemoMode(true)}
                    className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 text-xs font-semibold flex items-center gap-1.5 cursor-pointer"
                  >
                    <Sparkles className="w-3.5 h-3.5" /> Показать демо-каталог (6 книг)
                  </button>
                  <button
                    onClick={() => setActiveTab('ingestion')}
                    className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-xs font-semibold border border-slate-700 cursor-pointer"
                  >
                    Перейти к загрузке файла →
                  </button>
                </div>
              </div>
            )}

            {/* Карточки книг */}
            {entityType === 'books' && books.length > 0 && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {books.map((b) => (
                  <div
                    key={b.name}
                    onClick={() => handleInspectBook(b)}
                    className="p-5 rounded-xl bg-slate-900/90 border border-slate-800 hover:border-indigo-500/60 cursor-pointer transition group flex flex-col justify-between"
                  >
                    <div>
                      <div className="flex justify-between items-start gap-2">
                        <h4 className="font-semibold text-sm group-hover:text-indigo-400 transition leading-snug">
                          {b.name}
                        </h4>
                        <span className="text-xs font-mono px-2 py-0.5 rounded bg-slate-800 text-slate-300 shrink-0">
                          {b.yearOfPublication}
                        </span>
                      </div>
                      <p className="text-xs text-slate-400 mt-2">
                        {b.author || 'Автор не указан'} • {b.publisher || 'Без издательства'}
                      </p>
                    </div>
                    <div className="mt-4 pt-3 border-t border-slate-800/80 flex justify-between items-center text-xs text-slate-500">
                      <span className="font-mono text-[11px]">ISBN: {b.isbn || '—'}</span>
                      <span className="text-indigo-400 font-medium group-hover:translate-x-0.5 transition">
                        Проверить в кэше →
                      </span>
                    </div>
                  </div>
                ))}
              </div>
            )}

            {entityType === 'newspapers' && newspapers.length > 0 && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {newspapers.map((n) => (
                  <div key={n.name} className="p-5 rounded-xl bg-slate-900 border border-slate-800">
                    <h4 className="font-semibold">{n.name}</h4>
                    <p className="text-xs text-slate-400 mt-2">
                      Издательство: {n.publisher} • Год: {n.yearOfPublication}
                    </p>
                    <div className="mt-3 text-xs text-slate-500">ISSN: {n.issn || '—'}</div>
                  </div>
                ))}
              </div>
            )}

            {entityType === 'patents' && patents.length > 0 && (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {patents.map((p) => (
                  <div key={p.name} className="p-5 rounded-xl bg-slate-900 border border-slate-800">
                    <h4 className="font-semibold">{p.name}</h4>
                    <p className="text-xs text-slate-400 mt-2">
                      Изобретатель: {p.author} • Страна: {p.placeOfPublication}
                    </p>
                    <div className="mt-3 text-xs text-slate-500">
                      Рег. номер: {p.registrationNumber || '—'}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {/* ВКЛАДКА 2: БЕНЧМАРК */}
        {activeTab === 'benchmark' && (
          <div className="space-y-6">
            <div className="p-5 rounded-xl bg-slate-900/90 border border-slate-800 flex flex-wrap items-center justify-between gap-4">
              <div>
                <h2 className="text-base font-bold flex items-center gap-2">
                  <Flame className="w-5 h-5 text-orange-400" />
                  Сравнительный стресс-тест: IFS Predictive Cache vs LRU Cache
                </h2>
                <p className="text-xs text-slate-400 mt-1">
                  Генерация самоподобного трафика по распределению Ципфа и сравнение задержек с классическим вытеснением LRU
                </p>
              </div>

              <div className="flex flex-wrap items-center gap-3">
                <div className="flex items-center gap-2 bg-slate-950 border border-slate-800 rounded-lg px-3 py-1.5 text-xs">
                  <SlidersHorizontal className="w-3.5 h-3.5 text-slate-400" />
                  <span className="text-slate-400">Выборка:</span>
                  <select
                    value={requestsCount}
                    onChange={(e) => setRequestsCount(Number(e.target.value))}
                    className="bg-transparent font-semibold text-slate-200 focus:outline-none"
                  >
                    <option value={100} className="bg-slate-900">100 запросов</option>
                    <option value={300} className="bg-slate-900">300 запросов</option>
                    <option value={500} className="bg-slate-900">500 запросов</option>
                    <option value={1000} className="bg-slate-900">1000 запросов</option>
                  </select>
                </div>

                <button
                  onClick={handleWarmup}
                  className="px-4 py-2 rounded-lg bg-slate-800 hover:bg-slate-700 text-xs font-semibold border border-slate-700 cursor-pointer"
                >
                  Прогреть IFS-кэш
                </button>
                <button
                  onClick={handleRunBenchmark}
                  disabled={isRunningBenchmark}
                  className="px-4 py-2 rounded-lg bg-indigo-600 hover:bg-indigo-500 disabled:opacity-50 text-xs font-semibold flex items-center gap-2 cursor-pointer shadow-lg shadow-indigo-600/20"
                >
                  <Zap className="w-4 h-4" />
                  {isRunningBenchmark ? 'Выполняется тест...' : 'Запустить бенчмарк'}
                </button>
              </div>
            </div>

            {warmupStatus && (
              <div className="p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 text-xs flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4 shrink-0" />
                <span>{warmupStatus}</span>
              </div>
            )}

            {!benchmark && !isRunningBenchmark && (
              <div className="p-10 rounded-2xl bg-slate-900/50 border border-dashed border-slate-800 text-center">
                <Activity className="w-10 h-10 text-indigo-400/70 mx-auto mb-3" />
                <h3 className="text-sm font-bold text-slate-200">
                  Готов к запуску сравнительного профилирования
                </h3>
                <p className="text-xs text-slate-400 mt-1 max-w-md mx-auto">
                  Нажмите «Запустить бенчмарк», чтобы прогнать серию запросов через IFS-декоратор и классический LRU-кэш.
                </p>
              </div>
            )}

            {benchmark && (
              <>
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                  <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-xs text-slate-400">Ускорение IFS vs LRU</div>
                    <div className="text-2xl font-bold font-mono text-emerald-400 mt-1">
                      x{benchmark.speedupFactor}
                    </div>
                  </div>
                  <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-xs text-slate-400">IFS p95 Latency</div>
                    <div className="text-2xl font-bold font-mono text-indigo-400 mt-1">
                      {benchmark.ifsP95Ms} ms
                    </div>
                  </div>
                  <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-xs text-slate-400">LRU p95 Latency</div>
                    <div className="text-2xl font-bold font-mono text-amber-400 mt-1">
                      {benchmark.lruP95Ms} ms
                    </div>
                  </div>
                  <div className="p-4 rounded-xl bg-slate-900 border border-slate-800">
                    <div className="text-xs text-slate-400">Всего запросов (Zipf)</div>
                    <div className="text-2xl font-bold font-mono mt-1">{benchmark.totalRequests}</div>
                  </div>
                </div>

                {/* Два раздельных графика с правильным масштабом осей */}
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                  <div className="p-6 rounded-xl bg-slate-900 border border-slate-800">
                    <h3 className="text-sm font-semibold mb-1">
                      Перцентили задержек: p50 и p95 (мс)
                    </h3>
                    <p className="text-xs text-slate-400 mb-4">
                      Точечная отзывчивость одного запроса (меньше — лучше)
                    </p>
                    <div className="h-64">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart data={latencyChartData}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                          <XAxis dataKey="metric" stroke="#94a3b8" fontSize={12} />
                          <YAxis stroke="#94a3b8" fontSize={12} />
                          <Tooltip
                            contentStyle={{
                              backgroundColor: '#0f172a',
                              borderColor: '#334155',
                              fontSize: '12px',
                            }}
                          />
                          <Legend />
                          <Bar dataKey="IFS Predictive Cache" fill="#10b981" radius={[6, 6, 0, 0]} />
                          <Bar dataKey="LRU Cache" fill="#f59e0b" radius={[6, 6, 0, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  </div>

                  <div className="p-6 rounded-xl bg-slate-900 border border-slate-800">
                    <h3 className="text-sm font-semibold mb-1">
                      Суммарное время прогона пачки (Total Time, мс)
                    </h3>
                    <p className="text-xs text-slate-400 mb-4">
                      Общее время обработки {benchmark.totalRequests} самоподобных запросов
                    </p>
                    <div className="h-64">
                      <ResponsiveContainer width="100%" height="100%">
                        <BarChart data={totalTimeChartData}>
                          <CartesianGrid strokeDasharray="3 3" stroke="#1e293b" />
                          <XAxis dataKey="metric" stroke="#94a3b8" fontSize={12} />
                          <YAxis stroke="#94a3b8" fontSize={12} />
                          <Tooltip
                            contentStyle={{
                              backgroundColor: '#0f172a',
                              borderColor: '#334155',
                              fontSize: '12px',
                            }}
                          />
                          <Legend />
                          <Bar dataKey="IFS Predictive Cache" fill="#6366f1" radius={[6, 6, 0, 0]} />
                          <Bar dataKey="LRU Cache" fill="#f59e0b" radius={[6, 6, 0, 0]} />
                        </BarChart>
                      </ResponsiveContainer>
                    </div>
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {/* ВКЛАДКА 3: ЗАГРУЗКА (FILEWATCHER) */}
        {activeTab === 'ingestion' && (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <div className="lg:col-span-2 p-6 rounded-xl bg-slate-900 border border-slate-800 space-y-5">
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <Database className="w-6 h-6 text-indigo-400" />
                  <div>
                    <h3 className="font-bold">Потоковая загрузка в реактивный конвейер</h3>
                    <p className="text-xs text-slate-400">
                      Файл попадает в том /app/watch и асинхронно разбирается фоновым сервисом
                    </p>
                  </div>
                </div>

                <button
                  onClick={handleGenerateSampleCsv}
                  disabled={isUploading}
                  className="px-3.5 py-2 rounded-lg bg-indigo-600/20 hover:bg-indigo-600/30 text-indigo-300 border border-indigo-500/30 text-xs font-semibold flex items-center gap-1.5 cursor-pointer shrink-0"
                >
                  <FileSpreadsheet className="w-4 h-4" />
                  Отправить тестовый CSV
                </button>
              </div>

              <label className="flex flex-col items-center justify-center border-2 border-dashed border-slate-700 hover:border-indigo-500 rounded-xl p-10 cursor-pointer transition bg-slate-950/50">
                <UploadCloud className="w-10 h-10 text-indigo-400 mb-2" />
                <span className="text-sm font-medium text-slate-200">
                  {isUploading
                    ? 'Отправка файла на сервер...'
                    : 'Нажмите для выбора файла (books.xml или books_ready_for_postgres.csv)'}
                </span>
                <span className="text-xs text-slate-500 mt-1">
                  Либо нажмите «Отправить тестовый CSV» справа вверху для автозаполнения БД
                </span>
                <input type="file" className="hidden" onChange={handleFileUpload} />
              </label>

              {uploadResult && (
                <div className="p-3.5 rounded-xl bg-emerald-500/10 border border-emerald-500/30 text-emerald-300 text-xs flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 shrink-0" />
                  <span>{uploadResult}</span>
                </div>
              )}
            </div>

            {/* Схема архитектуры конвейера */}
            <div className="p-6 rounded-xl bg-slate-900 border border-slate-800 space-y-4">
              <h4 className="text-sm font-bold text-slate-200">Архитектура Ingestion-конвейера</h4>
              <div className="space-y-3 text-xs">
                <div className="p-3 rounded-lg bg-slate-950 border border-slate-800">
                  <div className="font-semibold text-indigo-400">1. Multipart HTTP Endpoint</div>
                  <div className="text-slate-400 mt-0.5">
                    Принимает поток байтов через Minimal API и атомарно записывает в Docker-том.
                  </div>
                </div>
                <div className="flex justify-center text-slate-600">
                  <ArrowRight className="w-4 h-4 rotate-90" />
                </div>
                <div className="p-3 rounded-lg bg-slate-950 border border-slate-800">
                  <div className="font-semibold text-emerald-400">
                    2. FileSystemWatcher + Queue
                  </div>
                  <div className="text-slate-400 mt-0.5">
                    Перехватывает событие создания файла и помещает задачу в конкурентную очередь.
                  </div>
                </div>
                <div className="flex justify-center text-slate-600">
                  <ArrowRight className="w-4 h-4 rotate-90" />
                </div>
                <div className="p-3 rounded-lg bg-slate-950 border border-slate-800">
                  <div className="font-semibold text-amber-400">
                    3. Dapper Bulk Insert & IFS Update
                  </div>
                  <div className="text-slate-400 mt-0.5">
                    Записывает издания в PostgreSQL и пересчитывает веса кэша по условиям ККТ.
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Журнал телеметрии запросов (внизу экрана) */}
        {traces.length > 0 && (
          <div className="p-4 rounded-xl bg-slate-900/70 border border-slate-800/80">
            <div className="flex items-center gap-2 text-xs font-semibold text-slate-400 mb-3">
              <Terminal className="w-4 h-4 text-indigo-400" />
              Живой журнал телеметрии (последние обращения к API)
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-2.5">
              {traces.map((t) => (
                <div
                  key={t.id}
                  className="px-3 py-2 rounded-lg bg-slate-950 border border-slate-800/80 flex items-center justify-between text-xs font-mono"
                >
                  <span className="truncate text-slate-300 pr-2">{t.action}</span>
                  <div className="flex items-center gap-2 shrink-0">
                    <span
                      className={`px-1.5 py-0.5 rounded text-[10px] ${
                        t.source === 'IFS-Cache'
                          ? 'bg-emerald-500/15 text-emerald-400'
                          : 'bg-amber-500/15 text-amber-400'
                      }`}
                    >
                      {t.source}
                    </span>
                    <span className="text-indigo-400">{t.latencyMs}ms</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}