import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useIntl } from "react-intl";
import { hasRole, useMe, type Role } from "../../api/auth";
import Spinner from "../Spinner/Spinner";
import ErrorMessage from "../ErrorMessage/ErrorMessage";
import EmptyState from "../EmptyState/EmptyState";

type Props = {
  children: ReactNode;
  // Optional role the user must have, e.g. Role.Organizer
  role?: Role;
};

export default function RequireAuth({ children, role }: Props) {
  const intl = useIntl();
  const { data: me, isPending, error, refetch, isFetching } = useMe();
  const location = useLocation();

  if (isPending) return <Spinner />;
  // getMe() returns null on 401, so an error here means the server is unreachable, not that the user is logged out.
  if (error)
    return (
      <ErrorMessage error={error} onRetry={refetch} retrying={isFetching} />
    );
  if (!me)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (role && !hasRole(me, role))
    return (
      <EmptyState message={intl.formatMessage({ id: "auth.forbidden" })} />
    );
  return children;
}
