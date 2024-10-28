create table if not exists file(
    id uuid primary key,
    created_at timestamp with time zone not null,
    modified_at timestamp with time zone,
    deleted_at timestamp with time zone,
    is_deleted boolean not null default false,
    file_data bytea,
    filemetadata_id UUID REFERENCES filemetadata(id)
)