import React from 'react';
import ReactDOM from 'react-dom/client';
import { AuthProvider } from 'react-oidc-context';
import { oidcConfig } from './authConfig';
import App from './App';
import './index.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <AuthProvider
      {...oidcConfig}
      onSigninCallback={() => {
        // Strip the OAuth code/state params from the URL after login
        window.history.replaceState({}, document.title, window.location.pathname);
      }}
    >
      <App />
    </AuthProvider>
  </React.StrictMode>,
);
