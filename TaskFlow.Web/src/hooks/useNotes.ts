import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import {
  getApiV1TaskItemsByTaskIdNotes,
  postApiV1TaskItemsByTaskIdNotes,
  putApiV1TaskItemsByTaskIdNotesById,
  deleteApiV1TaskItemsByTaskIdNotesById,
} from '@/api/client/sdk.gen'
import type { CreateNoteDto, UpdateNoteDto } from '@/api/client/types.gen'

export const noteKeys = {
  all: (taskId: number) => ['tasks', taskId, 'notes'] as const,
}

export function useNotesQuery(taskId: number) {
  return useQuery({
    queryKey: noteKeys.all(taskId),
    queryFn: () => getApiV1TaskItemsByTaskIdNotes({ path: { taskId } }),
  })
}

export function useCreateNoteMutation(taskId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (data: CreateNoteDto) =>
      postApiV1TaskItemsByTaskIdNotes({ path: { taskId }, body: data }),
    onSuccess: () => qc.invalidateQueries({ queryKey: noteKeys.all(taskId) }),
  })
}

export function useUpdateNoteMutation(taskId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateNoteDto }) =>
      putApiV1TaskItemsByTaskIdNotesById({ path: { taskId, id }, body: data }),
    onSuccess: () => qc.invalidateQueries({ queryKey: noteKeys.all(taskId) }),
  })
}

export function useDeleteNoteMutation(taskId: number) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (id: number) =>
      deleteApiV1TaskItemsByTaskIdNotesById({ path: { taskId, id } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: noteKeys.all(taskId) }),
  })
}
