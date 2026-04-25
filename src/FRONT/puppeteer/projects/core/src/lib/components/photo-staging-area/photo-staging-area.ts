import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { CdkDrag, CdkDragDrop, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
import { PropertyImage } from '../../models/property.model';
import { ImageCompressionService } from '../../services/image-compression.service';
import { CachedSrcDirective } from '../../directives/cached-src.directive';

export interface StagedFile {
  /** Unique identifier for CDK tracking */
  id: string;
  /** Compressed WebP file ready to upload */
  file: File;
  /** Key based on the *original* pre-compression file — used for deduplication */
  originalKey: string;
  /** `blob:` URL for local preview — revoked on removal/destroy */
  previewUrl: string;
  status: 'compressing' | 'ready';
}

@Component({
  selector: 'app-photo-staging-area',
  imports: [CdkDropList, CdkDrag, CachedSrcDirective],
  templateUrl: './photo-staging-area.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhotoStagingAreaComponent implements OnDestroy {
  readonly uploadedImages = input<PropertyImage[]>([]);
  readonly disabled = input<boolean>(false);

  /** Emits the current compressed-and-ready staged files whenever the list changes */
  readonly filesStaged = output<File[]>();
  /** Emits the image the user wants to delete from the uploaded set */
  readonly deleteImage = output<PropertyImage>();

  private readonly compression = inject(ImageCompressionService);

  protected readonly stagedFiles = signal<StagedFile[]>([]);
  protected readonly isDraggingOver = signal(false);

  protected readonly readyFiles = computed(() =>
    this.stagedFiles()
      .filter((f) => f.status === 'ready')
      .map((f) => f.file),
  );

  // ── File selection ──────────────────────────────────────────────────────────

  protected openFilePicker(input: HTMLInputElement): void {
    input.value = '';
    input.click();
  }

  protected onNativeInputChange(event: Event): void {
    const files = (event.target as HTMLInputElement).files;
    if (files) this.stageFiles(Array.from(files));
  }

  protected onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver.set(true);
  }

  protected onDragLeave(): void {
    this.isDraggingOver.set(false);
  }

  protected onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDraggingOver.set(false);
    const files = Array.from(event.dataTransfer?.files ?? []).filter((f) =>
      f.type.startsWith('image/'),
    );
    if (files.length) this.stageFiles(files);
  }

  private async stageFiles(rawFiles: File[]): Promise<void> {
    const imageFiles = rawFiles.filter((f) => f.type.startsWith('image/'));
    if (!imageFiles.length) return;

    // Deduplicate against already-staged items using the *original* file identity
    // (post-compression the entry's .file changes to a WebP, so we must not key off that)
    const existingKeys = new Set(
      this.stagedFiles().map((s) => s.originalKey),
    );

    const newEntries: StagedFile[] = imageFiles
      .filter((f) => !existingKeys.has(`${f.name}-${f.size}-${f.lastModified}`))
      .map((file) => ({
        id: `${Date.now()}-${Math.random()}`,
        file,
        originalKey: `${file.name}-${file.size}-${file.lastModified}`,
        previewUrl: URL.createObjectURL(file),
        status: 'compressing' as const,
      }));

    if (!newEntries.length) return;

    this.stagedFiles.update((current) => [...current, ...newEntries]);
    this.emitReady();

    // Compress each file and update status individually
    await Promise.all(
      newEntries.map(async (entry) => {
        try {
          const compressed = await this.compression.compress(entry.file);
          // Update the entry: revoke the original preview and generate one for the compressed file
          URL.revokeObjectURL(entry.previewUrl);
          const compressedPreview = URL.createObjectURL(compressed);
          this.stagedFiles.update((current) =>
            current.map((s) =>
              s.id === entry.id
                ? { ...s, file: compressed, previewUrl: compressedPreview, status: 'ready' }
                : s,
            ),
          );
        } catch {
          // Compression failed — keep original file as ready
          this.stagedFiles.update((current) =>
            current.map((s) => (s.id === entry.id ? { ...s, status: 'ready' } : s)),
          );
        }
        this.emitReady();
      }),
    );
  }

  // ── Staged file management ───────────────────────────────────────────────────

  protected removeStaged(entry: StagedFile): void {
    URL.revokeObjectURL(entry.previewUrl);
    this.stagedFiles.update((current) => current.filter((s) => s.id !== entry.id));
    this.emitReady();
  }

  protected reorderStaged(event: CdkDragDrop<StagedFile[]>): void {
    this.stagedFiles.update((current) => {
      const reordered = [...current];
      moveItemInArray(reordered, event.previousIndex, event.currentIndex);
      return reordered;
    });
    this.emitReady();
  }

  protected onDeleteUploaded(image: PropertyImage): void {
    this.deleteImage.emit(image);
  }

  private emitReady(): void {
    this.filesStaged.emit(this.readyFiles());
  }

  // ── Cleanup ──────────────────────────────────────────────────────────────────

  ngOnDestroy(): void {
    for (const entry of this.stagedFiles()) {
      URL.revokeObjectURL(entry.previewUrl);
    }
  }
}
