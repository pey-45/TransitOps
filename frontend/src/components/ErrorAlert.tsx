export function ErrorAlert({ message }: { message?: string }) {
  return message ? <div className="alert" role="alert">{message}</div> : null
}
