// Infraestrutura HTTP — usada por todos os services e pelo hook usePaginatedFetch
export interface BaseResponse<T> {
  Success: boolean;
  Message: string | null;
  ErrorDetails: string | null;
  Data: T;
  Errors: ErrorModel[] | null;
  IsError: boolean;
}

export interface BasePagedResponse<T> extends BaseResponse<T[]> {
  PageInfo: PageInfoRequest;
  Pages: number;
  ItemsTotal: number;
}

export interface ErrorModel {
  Property: string | null;
  Message: string[] | null;
}

export interface PageInfoRequest {
  PageNumber: number;
  PageSize: number;
}
