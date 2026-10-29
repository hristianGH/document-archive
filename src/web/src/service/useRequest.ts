import { useCallback, useRef, useState } from 'react';
import { toProblem, type Problem } from '../api/http';

export type RequestState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; problem: Problem };

// The ref blocks a second start before React re-renders the disabled button.
export function useRequest<TArgs extends unknown[], TResult>(action: (...args: TArgs) => Promise<TResult>) {
  const inFlight = useRef(false);
  const [state, setState] = useState<RequestState<TResult>>({ status: 'idle' });

  const run = useCallback(async (...args: TArgs) => {
    if (inFlight.current) {
      return;
    }

    inFlight.current = true;
    setState({ status: 'loading' });
    try {
      setState({ status: 'success', data: await action(...args) });
    } catch (error) {
      setState({ status: 'error', problem: toProblem(error) });
    } finally {
      inFlight.current = false;
    }
  }, [action]);

  return { state, run };
}
