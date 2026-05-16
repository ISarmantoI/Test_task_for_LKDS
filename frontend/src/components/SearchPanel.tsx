interface Organization { id: number; name: string; }

export interface SearchFilters {
  lastName: string; firstName: string; middleName: string;
  position: string; department: string; email: string;
  phone: string; organizationId: string;
}

interface SearchPanelProps {
  filters: SearchFilters;
  organizations: Organization[];
  onFiltersChange: (f: SearchFilters) => void;
  onSearch: () => void;
  onReset: () => void;
}

export const emptyFilters: SearchFilters = {
  lastName: '', firstName: '', middleName: '',
  position: '', department: '', email: '',
  phone: '', organizationId: '',
};

export default function SearchPanel({ filters, organizations, onFiltersChange, onSearch, onReset }: SearchPanelProps) {
  function handleChange(e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) {
    onFiltersChange({ ...filters, [e.target.name]: e.target.value });
  }

  const hasFilters = Object.values(filters).some(v => v !== '');

  return (
    <form className="search-panel" onSubmit={e => { e.preventDefault(); onSearch(); }}>
      <div className="search-panel-title">🔍 Фильтры поиска</div>

      <div className="search-fields">
        {[
          { id: 'lastName',  label: 'Фамилия',   placeholder: 'Иванов' },
          { id: 'firstName', label: 'Имя',        placeholder: 'Иван' },
          { id: 'middleName',label: 'Отчество',   placeholder: 'Иванович' },
          { id: 'position',  label: 'Должность',  placeholder: 'Менеджер' },
          { id: 'department',label: 'Отдел',      placeholder: 'Бухгалтерия' },
          { id: 'email',     label: 'Email',      placeholder: 'ivan@mail.ru' },
          { id: 'phone',     label: 'Телефон',    placeholder: '+7 (999)...' },
        ].map(({ id, label, placeholder }) => (
          <div className="search-field" key={id}>
            <label htmlFor={`sf-${id}`}>{label}</label>
            <input
              id={`sf-${id}`} name={id} type="text"
              value={(filters as unknown as Record<string, string>)[id]}
              onChange={handleChange} placeholder={placeholder}
            />
          </div>
        ))}

        <div className="search-field">
          <label htmlFor="sf-org">Организация</label>
          <select id="sf-org" name="organizationId" value={filters.organizationId} onChange={handleChange}>
            <option value="">Все организации</option>
            {organizations.map(o => (
              <option key={o.id} value={String(o.id)}>{o.name}</option>
            ))}
          </select>
        </div>
      </div>

      <div className="search-actions">
        {hasFilters && (
          <button type="button" className="btn btn-secondary" onClick={onReset}>
            Сбросить
          </button>
        )}
        <button type="submit" className="btn btn-primary">
          Найти
        </button>
      </div>
    </form>
  );
}
