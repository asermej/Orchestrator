const API_BASE =
  typeof window !== "undefined"
    ? process.env.NEXT_PUBLIC_TEST_ATS_API_URL || "http://localhost:5001"
    : process.env.NEXT_PUBLIC_TEST_ATS_API_URL || "http://localhost:5001";

async function getToken(): Promise<string | null> {
  if (typeof window === "undefined") return null;
  const res = await fetch("/api/auth/token");
  if (!res.ok) return null;
  const data = await res.json();
  return data.accessToken ?? null;
}

async function fetchWithAuth(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const token = await getToken();
  const headers = new Headers(options.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
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
