import { createContext, useCallback, useContext, useRef, useState, type ReactNode } from 'react';
import { CloseIcon } from './Icons';

interface ToastMessage {
  title: string;
  message: string;
}

interface ToastEntry extends ToastMessage {
  id: number;
}

const visibleMs = 8000;
const maxVisible = 4;

const ToastContext = createContext<(toast: ToastMessage) => void>(() => {});

export function useToast() {
  return useContext(ToastContext);
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<ToastEntry[]>([]);
  const nextId = useRef(0);

  const dismiss = useCallback((id: number) => setToasts(current => current.filter(toast => toast.id !== id)), []);

  const show = useCallback((toast: ToastMessage) => {
    const id = ++nextId.current;
    setToasts(current => [...current.slice(-(maxVisible - 1)), { ...toast, id }]);
    setTimeout(() => dismiss(id), visibleMs);
  }, [dismiss]);

  return (
    <ToastContext value={show}>
      {children}
      <div className="toasts">
        {toasts.map(toast => (
          <div key={toast.id} className="toast" role="alert">
            <div className="toast-body">
              <p className="toast-title">{toast.title}</p>
              <p className="toast-message">{toast.message}</p>
            </div>
            <button type="button" className="icon-button icon-button-quiet" aria-label="Dismiss" onClick={() => dismiss(toast.id)}>
              <CloseIcon />
            </button>
          </div>
        ))}
      </div>
    </ToastContext>
  );
}