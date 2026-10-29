import { useEffect, useState } from 'react';
import { getCabinets } from './api/archive';
import { Empty } from './components/Empty';
import { ErrorMessage } from './components/ErrorMessage';
import { Header } from './components/Header';
import { Loading } from './components/Loading';
import { Panel } from './components/Panel';
import { useRoute } from './service/route';
import { useSettings } from './service/settings';
import { useHealth } from './service/useHealth';
import { useRequest } from './service/useRequest';
import { CabinetView } from './view/CabinetView';
import { ExportView } from './view/ExportView';
import { UploadView } from './view/UploadView';

export function App() {
  const page = useRoute();
  const { settings, update } = useSettings();
  const { health, refresh } = useHealth(settings.healthIntervalSeconds);
  const { state, run } = useRequest(getCabinets, 'File cabinets could not be loaded');
  const [selectedName, setSelectedName] = useState('');

  useEffect(() => {
    void run();
  }, [run]);

  const renderPage = () => {
    if (state.status === 'error') {
      return (
        <Panel title="File cabinets" aside={<span className="badge badge-error">Failed</span>}>
          <ErrorMessage problem={state.problem} />
          <button type="button" className="button" onClick={() => void run()}>
            Try again
          </button>
        </Panel>
      );
    }

    if (state.status !== 'success') {
      return <Loading label="Loading file cabinets…" />;
    }

    const cabinets = state.data;
    const cabinet = cabinets.find(c => c.name === selectedName) ?? cabinets[0];
    if (!cabinet) {
      return <Empty>The archive has no file cabinets configured.</Empty>;
    }

    const props = { cabinets, cabinet, onSelectCabinet: setSelectedName };
    switch (page) {
      case 'upload':
        return <UploadView key={cabinet.name} {...props} />;
      case 'export':
        return <ExportView key={cabinet.name} {...props} />;
      default:
        return <CabinetView {...props} onReload={() => void run()} />;
    }
  };

  return (
    <>
      <Header page={page} health={health} onCheckHealth={refresh} settings={settings} onSettingsChange={update} />
      <main className="page">{renderPage()}</main>
    </>
  );
}