import { describe, it, expect, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { NoteForm } from './NoteForm'
import type { NoteResponseDto } from '@/api/client/types.gen'

describe('NoteForm', () => {
  it('renders content textarea', () => {
    render(<NoteForm onSubmit={vi.fn()} onCancel={vi.fn()} />)
    expect(screen.getByLabelText(/content/i)).toBeInTheDocument()
  })

  it('pre-fills content when editing', () => {
    const note: NoteResponseDto = { id: 1, content: 'Existing note', taskItemId: 1 }
    render(<NoteForm note={note} onSubmit={vi.fn()} onCancel={vi.fn()} />)
    expect(screen.getByDisplayValue('Existing note')).toBeInTheDocument()
  })

  it('calls onSubmit with content when submitted', async () => {
    const onSubmit = vi.fn()
    render(<NoteForm onSubmit={onSubmit} onCancel={vi.fn()} />)
    await userEvent.type(screen.getByLabelText(/content/i), 'My note')
    await userEvent.click(screen.getByRole('button', { name: /save/i }))
    expect(onSubmit).toHaveBeenCalledWith({ content: 'My note' })
  })

  it('calls onCancel when cancel is clicked', async () => {
    const onCancel = vi.fn()
    render(<NoteForm onSubmit={vi.fn()} onCancel={onCancel} />)
    await userEvent.click(screen.getByRole('button', { name: /cancel/i }))
    expect(onCancel).toHaveBeenCalled()
  })
})
