import type { UserManagerSettings } from 'oidc-client-ts';

const base = import.meta.env.VITE_IDENTITY_URL ?? 'http://localhost:5100';

export const oidcConfig: UserManagerSettings = {
  authority: base,
  client_id: 'admin-spa',
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: 'code',
  scope: 'openid profile email offline_access roles permissions',
  automaticSilentRenew: true,
  loadUserInfo: true,
};
