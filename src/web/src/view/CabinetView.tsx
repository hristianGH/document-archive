import { Empty } from '../components/Empty';
import { Endpoint } from '../components/Endpoint';
import { PageHead } from '../components/PageHead';
import { Panel } from '../components/Panel';
import { SelectField } from '../components/SelectField';
import { cabinetOptions } from '../service/fields';
import type { PageProps } from './PageProps';

export function CabinetView({ cabinets, cabinet, onSelectCabinet, onReload }: PageProps & { onReload: () => void }) {
  return (
    <>
      <PageHead title="File cabinets">The cabinets the archive knows and their index fields. Upload and Export use the cabinet you pick here.</PageHead>
      <div className="workspace">
        <Panel title="Request" aside={<Endpoint method="GET" path="/cabinets" />}>
          <div className="form">
            <SelectField label="Cabinet" value={cabinet.name} options={cabinetOptions(cabinets)} onChange={onSelectCabinet} />
            <div className="form-actions">
              <button type="button" className="button button-secondary" onClick={onReload}>
                Reload cabinets
              </button>
            </div>
          </div>
        </Panel>
        <Panel title="Response" aside={<span className="badge badge-success">{cabinet.fields.length} fields</span>}>
          {cabinet.fields.length === 0 ? (
            <Empty>This cabinet has no index fields.</Empty>
          ) : (
            <div className="table-scroll">
              <table className="table">
                <thead>
                  <tr>
                    <th>Label</th>
                    <th>Name</th>
                    <th>Type</th>
                    <th className="numeric">Max length</th>
                    <th>Writable</th>
                  </tr>
                </thead>
                <tbody>
                  {cabinet.fields.map(field => (
                    <tr key={field.name}>
                      <td>{field.label}</td>
                      <td>
                        <code>{field.name}</code>
                      </td>
                      <td>{field.type}</td>
                      <td className="numeric">{field.maxLength ?? '—'}</td>
                      <td>{field.writable ? 'Yes' : 'No'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Panel>
      </div>
    </>
  );
}