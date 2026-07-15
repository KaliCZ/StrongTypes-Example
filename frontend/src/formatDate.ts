const dateFormat = new Intl.DateTimeFormat("en", { dateStyle: "medium" });

export function formatDate(utcTimestamp: string): string {
  return dateFormat.format(new Date(utcTimestamp));
}
