import { useEffect, useState } from 'react';
import { getCabinets } from './api/archive';
import { ErrorMessage } from './components/ErrorMessage';
import { Loading } from './components/Loading';
import { useRequest } from './service/useRequest';
import { CabinetView } from './view/CabinetView';
import { ExportView } from './view/ExportView';
import { UploadView } from './view/UploadView';

export function App() {
  const { state, run } = useRequest(getCabinets);
  const [selectedName, setSelectedName] = useState('');

  useEffect(() => {
    void run();
  }, [run]);

  const cabinets = state.status === 'success' ? state.data : [];
  const selected = cabinets.find(cabinet => cabinet.name === selectedName);

  return (
    <main>
      <h1>Document archive</h1>
      {(state.status === 'idle' || state.status === 'loading') && <Loading label="Loading file cabinets…" />}
      {state.status === 'error' && (
        <>
          <ErrorMessage problem={state.problem} />
          <button type="button" onClick={() => void run()}>
            Try again
          </button>
        </>
      )}
      {state.status === 'success' && <CabinetView cabinets={cabinets} selected={selected} onSelect={setSelectedName} />}
      {selected && (
        <div key={selected.name}>
          <UploadView cabinet={selected} />
          <ExportView cabinet={selected} />
        </div>
      )}
    </main>
  );
}
