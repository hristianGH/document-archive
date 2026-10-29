import type { ReactNode } from 'react';
import type { Problem } from '../api/http';
import type { RequestState } from '../service/useRequest';
import { Empty } from './Empty';
import { ErrorMessage } from './ErrorMessage';
import { Loading } from './Loading';
import { Panel } from './Panel';

interface ResponsePanelProps<T> {
  state: RequestState<T>;
  idle: string;
  loading: string;
  children: (data: T) => ReactNode;
  error?: (problem: Problem) => ReactNode;
}

export function ResponsePanel<T>({ state, idle, loading, children, error }: ResponsePanelProps<T>) {
  return (
    <Panel title="Response" aside={<RequestBadge state={state} />}>
      {state.status === 'idle' && <Empty>{idle}</Empty>}
      {state.status === 'loading' && <Loading label={loading} />}
      {state.status === 'success' && children(state.data)}
      {state.status === 'error' && (
        <>
          <ErrorMessage problem={state.problem} />
          {error?.(state.problem)}
        </>
      )}
    </Panel>
  );
}

function RequestBadge({ state }: { state: RequestState<unknown> }) {
  switch (state.status) {
    case 'idle':
      return <span className="badge">Not sent</span>;
    case 'loading':
      return <span className="badge badge-busy">Sending…</span>;
    case 'success':
      return <span className="badge badge-success">Success</span>;
    case 'error':
      return <span className="badge badge-error">{state.problem.status >= 400 ? `Error ${state.problem.status}` : 'Failed'}</span>;
  }
}