import { baseApi as api } from "./baseApi";
const injectedRtkApi = api.injectEndpoints({
  endpoints: (build) => ({
    importWorkspace: build.mutation<
      ImportWorkspaceApiResponse,
      ImportWorkspaceApiArg
    >({
      query: (queryArg) => ({
        url: `/api/workspaces`,
        method: "POST",
        body: queryArg.body,
      }),
    }),
    getWorkspaceSummary: build.query<
      GetWorkspaceSummaryApiResponse,
      GetWorkspaceSummaryApiArg
    >({
      query: (queryArg) => ({ url: `/api/workspaces/${queryArg.id}` }),
    }),
    exportWorkspace: build.query<
      ExportWorkspaceApiResponse,
      ExportWorkspaceApiArg
    >({
      query: (queryArg) => ({ url: `/api/workspaces/${queryArg.id}/export` }),
    }),
    getCollections: build.query<
      GetCollectionsApiResponse,
      GetCollectionsApiArg
    >({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/collections`,
      }),
    }),
    getActions: build.query<GetActionsApiResponse, GetActionsApiArg>({
      query: (queryArg) => ({ url: `/api/workspaces/${queryArg.id}/actions` }),
    }),
    getVariables: build.query<GetVariablesApiResponse, GetVariablesApiArg>({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/variables`,
      }),
    }),
    getActionDetail: build.query<
      GetActionDetailApiResponse,
      GetActionDetailApiArg
    >({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/actions/${queryArg.actionId}`,
      }),
    }),
    renameAction: build.mutation<RenameActionApiResponse, RenameActionApiArg>({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/actions/${queryArg.actionId}`,
        method: "PATCH",
        body: queryArg.renameActionRequest,
      }),
    }),
    deleteAction: build.mutation<DeleteActionApiResponse, DeleteActionApiArg>({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/actions/${queryArg.actionId}`,
        method: "DELETE",
        params: {
          force: queryArg.force,
        },
      }),
    }),
    getGraph: build.query<GetGraphApiResponse, GetGraphApiArg>({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/graph`,
        params: {
          level: queryArg.level,
        },
      }),
    }),
    validateWorkspace: build.query<
      ValidateWorkspaceApiResponse,
      ValidateWorkspaceApiArg
    >({
      query: (queryArg) => ({ url: `/api/workspaces/${queryArg.id}/validate` }),
    }),
    copyOrMoveAction: build.mutation<
      CopyOrMoveActionApiResponse,
      CopyOrMoveActionApiArg
    >({
      query: (queryArg) => ({
        url: `/api/workspaces/${queryArg.id}/actions/${queryArg.actionId}/copy`,
        method: "POST",
        body: queryArg.copyActionRequest,
      }),
    }),
  }),
  overrideExisting: false,
});
export { injectedRtkApi as generatedApi };
export type ImportWorkspaceApiResponse = unknown;
export type ImportWorkspaceApiArg = {
  body: {
    file: IFormFile;
  };
};
export type GetWorkspaceSummaryApiResponse = unknown;
export type GetWorkspaceSummaryApiArg = {
  id: string;
};
export type ExportWorkspaceApiResponse = unknown;
export type ExportWorkspaceApiArg = {
  id: string;
};
export type GetCollectionsApiResponse = unknown;
export type GetCollectionsApiArg = {
  id: string;
};
export type GetActionsApiResponse = unknown;
export type GetActionsApiArg = {
  id: string;
};
export type GetVariablesApiResponse = unknown;
export type GetVariablesApiArg = {
  id: string;
};
export type GetActionDetailApiResponse = unknown;
export type GetActionDetailApiArg = {
  id: string;
  actionId: string;
};
export type RenameActionApiResponse = unknown;
export type RenameActionApiArg = {
  id: string;
  actionId: string;
  renameActionRequest: RenameActionRequest;
};
export type DeleteActionApiResponse = unknown;
export type DeleteActionApiArg = {
  id: string;
  actionId: string;
  force?: boolean;
};
export type GetGraphApiResponse = unknown;
export type GetGraphApiArg = {
  id: string;
  level?: string;
};
export type ValidateWorkspaceApiResponse = unknown;
export type ValidateWorkspaceApiArg = {
  id: string;
};
export type CopyOrMoveActionApiResponse = unknown;
export type CopyOrMoveActionApiArg = {
  id: string;
  actionId: string;
  copyActionRequest: CopyActionRequest;
};
export type IFormFile = Blob;
export type RenameActionRequest = {
  title: string;
};
export type CopyActionRequest = {
  targetCollection: string;
  mode: string;
  titlePrefix: null | string;
  referenceStrategy: string;
  replaceActionId?: null | string;
};
export const {
  useImportWorkspaceMutation,
  useGetWorkspaceSummaryQuery,
  useExportWorkspaceQuery,
  useGetCollectionsQuery,
  useGetActionsQuery,
  useGetVariablesQuery,
  useGetActionDetailQuery,
  useRenameActionMutation,
  useDeleteActionMutation,
  useGetGraphQuery,
  useValidateWorkspaceQuery,
  useCopyOrMoveActionMutation,
} = injectedRtkApi;
