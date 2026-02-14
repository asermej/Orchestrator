const API_BASE =
  typeof window !== "undefined"
    ? process.env.NEXT_PUBLIC_TEST_ATS_API_URL || "http://localhost:5001"
    : process.env.NEXT_PUBLIC_TEST_ATS_API_URL || "http://localhost:5001";

interface TokenInfo {
  accessToken: string | null;
  email?: string;
  name?: string;
}

async function getTokenInfo(): Promise<TokenInfo> {
  if (typeof window === "undefined") return { accessToken: null };
  const res = await fetch("/api/auth/token");
  if (!res.ok) return { accessToken: null };
  const data = await res.json();
  return {
    accessToken: data.accessToken ?? null,
    email: data.email ?? undefined,
    name: data.name ?? undefined,
  };
}

async function fetchWithAuth(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const info = await getTokenInfo();
  const headers = new Headers(options.headers);
  if (info.accessToken) headers.set("Authorization", `Bearer ${info.accessToken}`);
  if (info.email) headers.set("X-User-Email", info.email);
  if (info.name) headers.set("X-User-Name", info.name);
  return fetch(`${API_BASE}${path}`, { ...options, headers });
}

export const testAtsApi = {
  async get<T>(path: string): Promise<T> {
    const res = await fetchWithAuth(path);
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },
  async post<T>(path: string, body: unknown): Promise<T> {
    const res = await fetchWithAuth(path, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },
  async put<T>(path: string, body: unknown): Promise<T> {
    const res = await fetchWithAuth(path, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  },
  async delete(path: string): Promise<void> {
    const res = await fetchWithAuth(path, { method: "DELETE" });
    if (!res.ok) throw new Error(await res.text());
  },
};
