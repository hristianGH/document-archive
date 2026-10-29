import type { Cabinet } from '../api/types';
import { Empty } from '../components/Empty';

interface CabinetViewProps {
  cabinets: Cabinet[];
  selected: Cabinet | undefined;
  onSelect: (name: string) => void;
}

export function CabinetView({ cabinets, selected, onSelect }: CabinetViewProps) {
  if (cabinets.length === 0) {
    return <Empty>No file cabinets are configured.</Empty>;
  }

  return (
    <section>
      <h2>File cabinet</h2>
      <label>
        Cabinet{' '}
        <select value={selected?.name ?? ''} onChange={event => onSelect(event.target.value)}>
          <option value="" disabled>
            Choose a cabinet
          </option>
          {cabinets.map(cabinet => (
            <option key={cabinet.name} value={cabinet.name}>
              {cabinet.displayName}
            </option>
          ))}
        </select>
      </label>

      {selected &&
        (selected.fields.length === 0 ? (
          <Empty>This cabinet has no index fields.</Empty>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Label</th>
                <th>Name</th>
                <th>Type</th>
                <th>Max length</th>
                <th>Writable</th>
              </tr>
            </thead>
            <tbody>
              {selected.fields.map(field => (
                <tr key={field.name}>
                  <td>{field.label}</td>
                  <td>
                    <code>{field.name}</code>
                  </td>
                  <td>{field.type}</td>
                  <td>{field.maxLength ?? '—'}</td>
                  <td>{field.writable ? 'Yes' : 'No'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ))}
    </section>
  );
}
