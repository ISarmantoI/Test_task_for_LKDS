/** Пропсы компонента сообщения об ошибке. */
interface ErrorMessageProps {
  /** Текст ошибки для отображения пользователю. */
  message: string;
}

/**
 * Компонент отображения ошибки.
 * Показывает красный блок с иконкой предупреждения и текстом ошибки.
 * role="alert" гарантирует, что скринридер немедленно озвучит сообщение.
 */
export default function ErrorMessage({ message }: ErrorMessageProps) {
  return (
    <div className="error-message" role="alert">
      <span className="error-icon">⚠</span>
      {message}
    </div>
  );
}
