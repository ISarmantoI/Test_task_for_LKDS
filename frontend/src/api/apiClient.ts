// Базовый URL для всех запросов к API.
// Относительный путь работает как в dev-режиме (через Vite proxy), так и в production
// (React-статика раздаётся тем же ASP.NET Core сервером).
const BASE_URL = '/api';

/**
 * Обрабатывает HTTP-ответ: при статусе не 2xx бросает ошибку с текстом из тела ответа.
 * При статусе 204 No Content возвращает undefined.
 */
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    let message = `HTTP ошибка ${response.status}`;
    try {
      const body = await response.json();
      if (body?.error) {
        message = body.error;
      } else if (typeof body === 'string') {
        message = body;
      }
    } catch {
      // Если тело ответа не JSON — используем сообщение по умолчанию
    }
    throw new Error(message);
  }

  // 204 No Content — тело отсутствует, возвращаем undefined
  if (response.status === 204) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
}

/** GET-запрос. Возвращает десериализованный JSON-ответ. */
export async function get<T>(path: string): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`);
  return handleResponse<T>(response);
}

/** POST-запрос с JSON-телом. */
export async function post<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return handleResponse<T>(response);
}

/** PUT-запрос с JSON-телом. */
export async function put<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  return handleResponse<T>(response);
}

/** DELETE-запрос. Не возвращает тело ответа. */
export async function del(path: string): Promise<void> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'DELETE',
  });
  await handleResponse<void>(response);
}

/**
 * POST-запрос с телом FormData (multipart/form-data).
 * Используется для загрузки файлов (фото сотрудника).
 * Content-Type НЕ устанавливается вручную — браузер добавляет его автоматически
 * вместе с boundary для multipart.
 */
export async function postForm<T>(path: string, formData: FormData): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'POST',
    body: formData,
  });
  return handleResponse<T>(response);
}

/**
 * PUT-запрос с телом FormData (multipart/form-data).
 * Используется для обновления сотрудника с возможной заменой фото.
 */
export async function putForm<T>(path: string, formData: FormData): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, {
    method: 'PUT',
    body: formData,
  });
  return handleResponse<T>(response);
}
