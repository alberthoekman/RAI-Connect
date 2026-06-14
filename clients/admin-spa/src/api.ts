const BASE = import.meta.env.VITE_IDENTITY_URL ?? 'http://localhost:5100';

async function request<T>(
  path: string,
  token: string,
  options: RequestInit = {},
): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
      ...(options.headers ?? {}),
    },
  });
  if (!res.ok) {
    const text = await res.text().catch(() => '');
    throw new Error(`${res.status}: ${text || res.statusText}`);
  }
  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

// -- Users ------------------------------------------------------------------
export interface UserDto {
  id: string;
  email: string;
  displayName: string | null;
  roles?: string[];
}

export const getUsers = (token: string) =>
  request<UserDto[]>('/api/users', token);

export const getUser = (id: string, token: string) =>
  request<UserDto>(`/api/users/${id}`, token);

export const createUser = (
  data: { email: string; displayName: string; password: string },
  token: string,
) =>
  request<UserDto>('/api/users', token, {
    method: 'POST',
    body: JSON.stringify(data),
  });

export const deleteUser = (id: string, token: string) =>
  request<void>(`/api/users/${id}`, token, { method: 'DELETE' });

export const assignRole = (userId: string, roleName: string, token: string) =>
  request<void>(`/api/users/${userId}/roles`, token, {
    method: 'POST',
    body: JSON.stringify({ roleName }),
  });

export const removeRole = (userId: string, roleName: string, token: string) =>
  request<void>(`/api/users/${userId}/roles/${roleName}`, token, {
    method: 'DELETE',
  });

// -- Roles ------------------------------------------------------------------
export interface RoleDto {
  id: string;
  name: string;
  permissions: { id: number; name: string }[];
}

export const getRoles = (token: string) =>
  request<RoleDto[]>('/api/roles', token);

export const createRole = (name: string, token: string) =>
  request<RoleDto>('/api/roles', token, {
    method: 'POST',
    body: JSON.stringify({ name }),
  });

export const grantPermission = (
  roleId: string,
  permissionId: number,
  token: string,
) => request<void>(`/api/roles/${roleId}/permissions/${permissionId}`, token, { method: 'POST' });

export const revokePermission = (
  roleId: string,
  permissionId: number,
  token: string,
) => request<void>(`/api/roles/${roleId}/permissions/${permissionId}`, token, { method: 'DELETE' });

// -- Permissions ------------------------------------------------------------
export interface PermissionDto {
  id: number;
  name: string;
  description: string | null;
}

export const getPermissions = (token: string) =>
  request<PermissionDto[]>('/api/permissions', token);

// -- Me ---------------------------------------------------------------------
export interface MeDto {
  id: string;
  email: string;
  displayName: string | null;
  roles: string[];
  permissions: string[];
}

export const getMe = (token: string) => request<MeDto>('/api/me', token);

export const getProtected = (token: string) =>
  request<{ message: string }>('/api/me/protected', token);
