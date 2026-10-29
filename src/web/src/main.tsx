import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { ToastProvider } from './components/Toast';
import { loadConfig } from './config';
import './styles.css';

const root = createRoot(document.getElementById('root')!);

loadConfig()
  .then(() =>
    root.render(
      <StrictMode>
        <ToastProvider>
          <App />
        </ToastProvider>
      </StrictMode>
    )
  )
  .catch((error: Error) => root.render(<p className="callout callout-error boot-error">{error.message}</p>));