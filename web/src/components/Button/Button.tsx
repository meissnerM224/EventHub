import type { ButtonHTMLAttributes } from "react";
import { Link, type LinkProps } from "react-router-dom";
import styles from "./Button.module.css";

export type ButtonVariant = "primary" | "secondary" | "warning";
export type ButtonSize = "md" | "sm";

type StyleProps = {
  variant?: ButtonVariant;
  size?: ButtonSize;
};

// One look for <button> and <Link>. Layout (margin, alignment) stays with the caller via className.
function classes(variant: ButtonVariant, size: ButtonSize, extra?: string) {
  return [styles.button, styles[variant], size === "sm" && styles.sm, extra]
    .filter(Boolean)
    .join(" ");
}

type ButtonProps = StyleProps & ButtonHTMLAttributes<HTMLButtonElement>;

export function Button({
  variant = "primary",
  size = "md",
  type = "button",
  className,
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={classes(variant, size, className)}
      {...rest}
    />
  );
}

type ButtonLinkProps = StyleProps & LinkProps;

export function ButtonLink({
  variant = "primary",
  size = "md",
  className,
  ...rest
}: ButtonLinkProps) {
  return <Link className={classes(variant, size, className)} {...rest} />;
}
