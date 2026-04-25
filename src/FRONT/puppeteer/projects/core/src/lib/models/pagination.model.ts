export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface PaginationParams {
  page?: number;
  pageSize?: number;
}

/** Server slices in memory; use a large window until callers pass real pagination. */
export const LIST_FETCH_PAGE_SIZE = 1000;
