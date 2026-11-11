/** One row from GET /api/admin/audit. */
export interface AuditEntry {
  id: number;
  username: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  atUtc: string;
  details: string | null;
}
