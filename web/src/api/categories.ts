import { useQuery } from "@tanstack/react-query";
import { z } from "zod";
import { apiGet } from "./client";
import { categorySchema } from "./types";

const categoryListSchema = z.array(categorySchema);

export const getCategories = () =>
  apiGet("/api/categories", (data) => categoryListSchema.parse(data));

export const useCategories = () =>
  useQuery({
    queryKey: ["categories"],
    queryFn: getCategories,
    staleTime: 5 * 60_000, // categories rarely change
  });
