/**
 * Индикатор загрузки (spinner).
 * Отображается во время асинхронных запросов к API.
 * role="status" и aria-label обеспечивают доступность для скринридеров.
 */
export default function LoadingSpinner() {
  return (
    <div className="spinner-container" role="status" aria-label="Загрузка...">
      <div className="spinner" />
    </div>
  );
}
