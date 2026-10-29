interface Option {
  value: string;
  label: string;
}

interface SelectFieldProps {
  label: string;
  value: string;
  options: Option[];
  onChange: (value: string) => void;
  disabled?: boolean;
  hint?: string;
}

export function SelectField({ label, value, options, onChange, disabled, hint }: SelectFieldProps) {
  return (
    <label className="field">
      <span className="field-label">{label}</span>
      <select className="control" value={value} onChange={event => onChange(event.target.value)} disabled={disabled}>
        {options.map(option => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {hint && <span className="field-hint">{hint}</span>}
    </label>
  );
}