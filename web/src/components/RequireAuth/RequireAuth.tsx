import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";
import { useMe } from "../../api/auth";
import Spinner from "../Spinner/Spinner";

export default function RequireAuth({ children }: { children: ReactNode }) {
  const { data: me, isPending } = useMe();
  const location = useLocation();

  if (isPending) return <Spinner />;
  if (!me)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  return children;
}
