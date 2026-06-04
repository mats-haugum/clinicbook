// Reads the { message: "..." } body that the backend sends for every business-logic
// error (400, 404, 409, etc.). Falls back to a generic string when the body doesn't
// contain a message field — this happens for ASP.NET Core model-binding failures,
// which use the ProblemDetails shape ({ errors: { ... } }) instead.
export function extractApiError(err: unknown, fallback: string): string {
  const data = (err as { response?: { data?: { message?: string } } }).response?.data
  return data?.message ?? fallback
}
