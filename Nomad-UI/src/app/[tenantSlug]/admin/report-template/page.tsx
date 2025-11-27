"use client";

import React, { useEffect, useState } from "react";

interface Props {
  params: { tenantSlug: string };
}

export default function ReportTemplateEditor({ params }: Props) {
  const { tenantSlug } = params;
  const [html, setHtml] = useState<string>("");
  const [loading, setLoading] = useState<boolean>(false);
  const [companyLogo, setCompanyLogo] = useState<File | null>(null);
  const [coverImage, setCoverImage] = useState<File | null>(null);

  useEffect(() => {
    // Load existing template
    async function load() {
      setLoading(true);
      try {
        const res = await fetch(`/api/${tenantSlug}/report-template`);
        if (res.ok) {
          const text = await res.text();
          setHtml(text);
        } else {
          // no template yet or error
          console.warn("Failed to load template", res.status);
        }
      } catch (e) {
        console.error(e);
      } finally {
        setLoading(false);
      }
    }
    load();
  }, [tenantSlug]);

  const onSave = async () => {
    setLoading(true);
    try {
      const fd = new FormData();
      fd.append("html", html || "");
      if (companyLogo) fd.append("companyLogo", companyLogo);
      if (coverImage) fd.append("coverImage", coverImage);

      const res = await fetch(`/api/${tenantSlug}/report-template`, {
        method: "POST",
        body: fd,
      });

      if (res.ok) {
        alert("Template saved successfully.");
      } else {
        const txt = await res.text();
        alert("Save failed: " + res.status + " - " + txt);
      }
    } catch (e) {
      console.error(e);
      alert("Save failed: " + e);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div style={{ padding: 20 }}>
      <h1>Report Template Editor</h1>
      <p style={{ marginBottom: 12 }}>Tenant: <strong>{tenantSlug}</strong></p>

      <div style={{ marginBottom: 12 }}>
        <label style={{ display: "block", marginBottom: 6 }}>Company Logo</label>
        <input
          type="file"
          accept="image/*"
          onChange={(e) => setCompanyLogo(e.target.files ? e.target.files[0] : null)}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <label style={{ display: "block", marginBottom: 6 }}>Cover Image</label>
        <input
          type="file"
          accept="image/*"
          onChange={(e) => setCoverImage(e.target.files ? e.target.files[0] : null)}
        />
      </div>

      <div style={{ marginBottom: 12 }}>
        <button onClick={onSave} disabled={loading} style={{ marginRight: 8 }}>
          {loading ? "Saving..." : "Save Template"}
        </button>
        <button
          onClick={() => {
            // simple preview: open a new tab with the current html
            const w = window.open();
            if (w) {
              w.document.open();
              w.document.write(html || "<p>(empty)</p>");
              w.document.close();
            }
          }}
        >
          Preview
        </button>
      </div>

      <div>
        <textarea
          value={html}
          onChange={(e) => setHtml(e.target.value)}
          style={{ width: "100%", height: "60vh", fontFamily: "monospace", fontSize: 13 }}
        />
      </div>
    </div>
  );
}
