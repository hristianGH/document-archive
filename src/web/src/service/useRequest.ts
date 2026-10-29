import { useCallback, useRef, useState } from 'react';
import { summarize, toProblem, type Problem } from '../api/http';
import { useToast } from '../components/Toast';

export type RequestState<T> =
  | { status: 'idle' }
  | { status: 'loading' }
  | { status: 'success'; data: T }
  | { status: 'error'; problem: Problem };

// The ref blocks a second start before React re-renders the disabled button.
export function useRequest<TArgs extends unknown[], TResult>(action: (...args: TArgs) => Promise<TResult>, errorTitle: string) {
  const inFlight = useRef(false);
  const [state, setState] = useState<RequestState<TResult>>({ status: 'idle' });
  const showToast = useToast();

  const run = useCallback(async (...args: TArgs) => {
    if (inFlight.current) {
      return;
    }

    inFlight.current = true;
    setState({ status: 'loading' });
    try {
      setState({ status: 'success', data: await action(...args) });
    } catch (error) {
      const problem = toProblem(error);
      setState({ status: 'error', problem });
      showToast({ title: errorTitle, message: summarize(problem) });
    } finally {
      inFlight.current = false;
    }
  }, [action, errorTitle, showToast]);

  return { state, run };
}