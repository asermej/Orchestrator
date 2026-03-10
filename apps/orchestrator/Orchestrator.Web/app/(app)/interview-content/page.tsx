"use client";

import { useEffect, useState, useCallback } from "react";
import { useUser } from "@auth0/nextjs-auth0/client";
import { useRouter, useSearchParams } from "next/navigation";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  Loader2,
  ChevronRight,
  Plus,
  Pencil,
  ArrowLeft,
  FileQuestion,
  ListChecks,
  Shield,
  User,
  Trash2,
  Save,
  Sparkles,
  Star,
  Copy,
} from "lucide-react";
import {
  fetchRoleTemplates,
  fetchRoleTemplateDetail,
  createRoleTemplate,
  updateRoleTemplate,
  deleteRoleTemplate,
  cloneRoleTemplate,
  createCompetency,
  updateCompetency,
  deleteCompetency,
  fetchUniversalRubric,
  aiSuggestCompetencies,
  aiSuggestCanonicalExample,
  type RoleTemplate,
  type RoleTemplateDetail,
  type Competency,
  type UniversalRubricLevel,
} from "../competency-frameworks/actions";
import { ApiClientError } from "@/lib/api-types";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

type ViewMode = "role" | "rubric" | "questions" | null;
type ContentTab = "local" | "inherited" | "system";

function snakeToTitleCase(s: string): string {
  return s.split("_").map((w) => w.charAt(0).toUpperCase() + w.slice(1)).join(" ");
}

function visibilityScopeLabel(scope?: string): string | null {
  switch (scope) {
    case "organization_and_descendants":
      return "Shared with sub-orgs";
    case "descendants_only":
      return "Sub-orgs only";
    default:
      return null;
  }
}

export default function InterviewContentPage() {
  const { user, isLoading: isUserLoading } = useUser();
  const router = useRouter();
  const searchParams = useSearchParams();

  const roleKey = searchParams.get("role");
  const competencyId = searchParams.get("competency");
  const view = (searchParams.get("view") as ViewMode) || null;

  const [activeTab, setActiveTab] = useState<ContentTab>("local");
  const [roles, setRoles] = useState<RoleTemplate[]>([]);
  const [roleDetail, setRoleDetail] = useState<RoleTemplateDetail | null>(null);
  const [isLoadingRoles, setIsLoadingRoles] = useState(true);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [isCloning, setIsCloning] = useState<string | null>(null);
  const [roleToDelete, setRoleToDelete] = useState<RoleTemplate | null>(null);

  const [createRoleOpen, setCreateRoleOpen] = useState(false);
  const [newRoleName, setNewRoleName] = useState("");
  const [newRoleIndustry, setNewRoleIndustry] = useState("");
  const [newRoleVisibilityScope, setNewRoleVisibilityScope] = useState("organization_only");
  const [isCreatingRole, setIsCreatingRole] = useState(false);

  const [addCompetencyOpen, setAddCompetencyOpen] = useState(false);
  const [newCompName, setNewCompName] = useState("");
  const [newCompDescription, setNewCompDescription] = useState("");
  const [newCompWeight, setNewCompWeight] = useState(0);
  const [isAddingCompetency, setIsAddingCompetency] = useState(false);
  const [isGeneratingCompetencies, setIsGeneratingCompetencies] = useState(false);
  const [compToDelete, setCompToDelete] = useState<Competency | null>(null);

  const updateUrl = useCallback(
    (updates: { role?: string | null; competency?: string | null; view?: ViewMode }) => {
      const p = new URLSearchParams(searchParams.toString());
      if (updates.role !== undefined) {
        if (updates.role) p.set("role", updates.role);
        else p.delete("role");
      }
      if (updates.competency !== undefined) {
        if (updates.competency) p.set("competency", updates.competency);
        else p.delete("competency");
      }
      if (updates.view !== undefined) {
        if (updates.view) p.set("view", updates.view);
        else p.delete("view");
      }
      router.replace(`/interview-content?${p.toString()}`, { scroll: false });
    },
    [router, searchParams]
  );

  useEffect(() => {
    if (!isUserLoading && !user) router.push("/api/auth/login");
  }, [user, isUserLoading, router]);

  const loadRoles = useCallback(async () => {
    if (!user) return;
    try {
      setIsLoadingRoles(true);
      setError(null);
      const data = await fetchRoleTemplates(activeTab);
      setRoles(data);
    } catch (err) {
      console.error(err);
      setError("Failed to load roles.");
    } finally {
      setIsLoadingRoles(false);
    }
  }, [user, activeTab]);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  useEffect(() => {
    if (!roleKey || !user) {
      setRoleDetail(null);
      return;
    }
    (async () => {
      try {
        setIsLoadingDetail(true);
        const data = await fetchRoleTemplateDetail(roleKey);
        setRoleDetail(data);
      } catch (err) {
        console.error(err);
        setRoleDetail(null);
      } finally {
        setIsLoadingDetail(false);
      }
    })();
  }, [roleKey, user]);

  const selectedCompetency = roleDetail?.competencies?.find((c) => c.id === competencyId) ?? null;
  const isRoleReadOnly = roleDetail ? (roleDetail.source === "system" || roleDetail.isInherited) : false;

  const handleCreateRole = async () => {
    if (!newRoleName.trim() || !newRoleIndustry.trim()) return;
    setIsCreatingRole(true);
    setError(null);
    try {
      const created = await createRoleTemplate({
        roleName: newRoleName.trim(),
        industry: newRoleIndustry.trim(),
        visibilityScope: newRoleVisibilityScope,
      });
      setRoles((prev) => [...prev, created]);
      setNewRoleName("");
      setNewRoleIndustry("");
      setNewRoleVisibilityScope("organization_only");
      setCreateRoleOpen(false);
      updateUrl({ role: created.roleKey, competency: null, view: null });
      const detail = await fetchRoleTemplateDetail(created.roleKey);
      setRoleDetail(detail);
    } catch (err) {
      console.error(err);
      setError("Failed to create role.");
    } finally {
      setIsCreatingRole(false);
    }
  };

  const handleCloneRole = async (roleId: string) => {
    setIsCloning(roleId);
    setError(null);
    try {
      await cloneRoleTemplate(roleId);
      setActiveTab("local");
    } catch (err) {
      console.error(err);
      const message =
        err instanceof ApiClientError && err.error?.message
          ? err.error.message
          : "Failed to clone role.";
      setError(message);
    } finally {
      setIsCloning(null);
    }
  };

  const confirmDeleteRole = async () => {
    if (!roleToDelete) return;
    try {
      await deleteRoleTemplate(roleToDelete.id);
      if (roleKey === roleToDelete.roleKey) {
        setRoleDetail(null);
        updateUrl({ role: null, competency: null, view: null });
      }
      setRoleToDelete(null);
      loadRoles();
    } catch (err) {
      console.error(err);
      setError("Failed to delete role.");
      setRoleToDelete(null);
    }
  };

  const handleAddCompetency = async () => {
    if (!roleDetail || !newCompName.trim()) return;
    setIsAddingCompetency(true);
    setError(null);
    try {
      const nextOrder = Math.max(0, ...roleDetail.competencies.map((c) => c.displayOrder)) + 1;
      await createCompetency(roleDetail.id, {
        name: newCompName.trim(),
        description: newCompDescription.trim() || undefined,
        canonicalExample: "e.g. Tell me about a time when you demonstrated this.",
        defaultWeight: newCompWeight,
        isRequired: true,
        displayOrder: nextOrder,
      });
      const detail = await fetchRoleTemplateDetail(roleDetail.roleKey);
      setRoleDetail(detail);
      setNewCompName("");
      setNewCompDescription("");
      setNewCompWeight(0);
      setAddCompetencyOpen(false);
    } catch (err) {
      console.error(err);
      setError("Failed to add competency.");
    } finally {
      setIsAddingCompetency(false);
    }
  };

  const handleGenerateCompetencies = async () => {
    if (!roleDetail) return;
    setIsGeneratingCompetencies(true);
    setError(null);
    try {
      const suggestions = await aiSuggestCompetencies(roleDetail.roleName, roleDetail.industry);
      if (suggestions.length === 0) {
        setError("No competencies were suggested. Try adding them manually.");
        return;
      }
      const existingNames = new Set(roleDetail.competencies.map((c) => c.name.trim().toLowerCase()));
      const toCreate = suggestions.filter((s) => !existingNames.has(s.name.trim().toLowerCase()));
      if (toCreate.length === 0) {
        setError("Suggested competencies already exist for this role.");
        return;
      }
      const sum = toCreate.reduce((a, s) => a + s.defaultWeight, 0);
      const normalizedWeights = sum > 0
        ? toCreate.map((s, i) =>
            i === toCreate.length - 1
              ? 100 - toCreate.slice(0, -1).reduce((acc, x, j) => acc + Math.round((x.defaultWeight / sum) * 100), 0)
              : Math.round((s.defaultWeight / sum) * 100))
        : toCreate.map((_, i) => (i === toCreate.length - 1 ? 100 - 33 * (toCreate.length - 1) : 33));
      const startOrder = roleDetail.competencies.length;
      const roleContext = `${roleDetail.roleName} (${roleDetail.industry})`;

      for (let i = 0; i < toCreate.length; i++) {
        const created = await createCompetency(roleDetail.id, {
          name: toCreate[i].name.trim(),
          description: toCreate[i].description?.trim() || undefined,
          canonicalExample: "e.g. Tell me about a time when you demonstrated this.",
          defaultWeight: Math.max(0, Math.min(100, normalizedWeights[i] ?? 33)),
          isRequired: true,
          displayOrder: startOrder + i,
        });

        const suggestedExample = await aiSuggestCanonicalExample(created.name, roleContext, created.description);
        await updateCompetency(created.id, roleDetail.id, {
          name: created.name,
          description: created.description ?? undefined,
          canonicalExample: suggestedExample || (created.canonicalExample ?? "e.g. Tell me about a time when you demonstrated this."),
          defaultWeight: created.defaultWeight,
          isRequired: created.isRequired,
          displayOrder: created.displayOrder,
        });
      }

      const detail = await fetchRoleTemplateDetail(roleDetail.roleKey);
      setRoleDetail(detail);
    } catch (err) {
      console.error(err);
      const message = err instanceof ApiClientError && err.error?.message
        ? err.error.message
        : "Failed to generate competencies.";
      setError(message);
    } finally {
      setIsGeneratingCompetencies(false);
    }
  };

  const confirmDeleteCompetency = async () => {
    if (!compToDelete || !roleDetail) return;
    try {
      await deleteCompetency(compToDelete.id, roleDetail.id);
      if (compToDelete.id === competencyId) {
        updateUrl({ role: roleKey, competency: null, view: null });
      }
      setCompToDelete(null);
      const detail = await fetchRoleTemplateDetail(roleDetail.roleKey);
      setRoleDetail(detail);
    } catch (err) {
      console.error(err);
      setError("Failed to delete competency.");
      setCompToDelete(null);
    }
  };

  const leftColumnMode: "roles" | "competencies" =
    roleKey ? "competencies" : "roles";

  if (isUserLoading || !user) {
    return (
      <div className="min-h-screen bg-background flex items-center justify-center">
        <Loader2 className="h-8 w-8 animate-spin" />
      </div>
    );
  }

  return (
    <div className="container mx-auto px-4 py-8">
      <div className="mb-6">
        <h1 className="text-3xl font-bold">Interview Content</h1>
        <p className="text-muted-foreground mt-1">
          Manage roles, competencies, and structured interview questions.
        </p>
      </div>

      {error && (
        <div className="mb-4 p-4 bg-destructive/10 text-destructive rounded-lg text-sm">{error}</div>
      )}

      {leftColumnMode === "roles" ? (
        <div className="w-full space-y-6">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
            <div>
              <h2 className="text-2xl font-semibold tracking-tight">Roles</h2>
              <p className="text-muted-foreground mt-1">
                Select a role to manage its competencies and interview questions.
              </p>
            </div>
            {activeTab === "local" && !isLoadingRoles && (
              createRoleOpen ? (
                <Card className="sm:min-w-[320px]">
                  <CardContent className="py-4 space-y-3">
                    <Label className="text-xs font-medium">New role</Label>
                    <Input
                      value={newRoleName}
                      onChange={(e) => setNewRoleName(e.target.value)}
                      placeholder="e.g. Lube Technician"
                      className="h-9"
                    />
                    <Input
                      value={newRoleIndustry}
                      onChange={(e) => setNewRoleIndustry(e.target.value)}
                      placeholder="Industry (e.g. Auto, healthcare)"
                      className="h-9"
                    />
                    <div className="space-y-2 pt-1">
                      <Label className="text-xs font-medium">Visibility</Label>
                      {[
                        { value: "organization_only", label: "This organization only" },
                        { value: "organization_and_descendants", label: "This organization and sub-organizations" },
                        { value: "descendants_only", label: "Sub-organizations only" },
                      ].map((opt) => (
                        <label
                          key={opt.value}
                          className={`flex items-center gap-2 rounded-md border p-2 cursor-pointer text-sm transition-colors ${
                            newRoleVisibilityScope === opt.value
                              ? "border-primary bg-primary/5"
                              : "border-border hover:bg-muted/50"
                          }`}
                        >
                          <input
                            type="radio"
                            name="visibility"
                            value={opt.value}
                            checked={newRoleVisibilityScope === opt.value}
                            onChange={(e) => setNewRoleVisibilityScope(e.target.value)}
                            className="accent-primary"
                          />
                          {opt.label}
                        </label>
                      ))}
                    </div>
                    <div className="flex gap-2 pt-1">
                      <Button size="sm" variant="outline" onClick={() => setCreateRoleOpen(false)}>
                        Cancel
                      </Button>
                      <Button
                        size="sm"
                        onClick={handleCreateRole}
                        disabled={isCreatingRole || !newRoleName.trim() || !newRoleIndustry.trim()}
                      >
                        {isCreatingRole ? <Loader2 className="h-4 w-4 animate-spin" /> : "Create"}
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ) : (
                <Button className="gap-2 shrink-0" onClick={() => setCreateRoleOpen(true)}>
                  <Plus className="h-4 w-4" /> Create role
                </Button>
              )
            )}
          </div>

          {/* Tabs */}
          <div className="flex gap-2 border-b">
            {(["local", "inherited", "system"] as ContentTab[]).map((tab) => (
              <button
                key={tab}
                type="button"
                onClick={() => {
                  setActiveTab(tab);
                  setCreateRoleOpen(false);
                }}
                className={`px-4 py-2 text-sm font-medium border-b-2 transition-colors -mb-px ${
                  activeTab === tab
                    ? "border-primary text-foreground"
                    : "border-transparent text-muted-foreground hover:text-foreground hover:border-muted-foreground/30"
                }`}
              >
                {tab.charAt(0).toUpperCase() + tab.slice(1)}
              </button>
            ))}
          </div>

          {isLoadingRoles ? (
            <div className="flex justify-center py-16">
              <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
            </div>
          ) : roles.length === 0 ? (
            <Card className="border-dashed">
              <CardContent className="py-16 text-center">
                <p className="text-muted-foreground mb-4">
                  {activeTab === "local"
                    ? "No local roles yet."
                    : activeTab === "inherited"
                      ? "No inherited roles from parent organizations."
                      : "No system roles available."}
                </p>
                {activeTab === "local" && (
                  <Button onClick={() => setCreateRoleOpen(true)} className="gap-2">
                    <Plus className="h-4 w-4" /> Create your first role
                  </Button>
                )}
              </CardContent>
            </Card>
          ) : (
            <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-3 gap-4">
              {roles.map((role) => {
                const scopeLabel = visibilityScopeLabel(role.visibilityScope);
                const showClone = activeTab === "inherited" || activeTab === "system";
                return (
                  <Card
                    key={role.id}
                    className={`group cursor-pointer transition-all duration-200 hover:shadow-md hover:border-primary/30 ${
                      roleKey === role.roleKey ? "border-primary bg-primary/5 shadow-sm" : ""
                    }`}
                    onClick={() => updateUrl({ role: role.roleKey, competency: null, view: null })}
                  >
                    <CardContent className="p-5">
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0 flex-1">
                          <p className="font-semibold text-base truncate">{role.roleName}</p>
                          <div className="flex items-center gap-2 mt-1.5 flex-wrap">
                            {activeTab === "system" && (
                              <Badge variant="outline" className="text-xs font-normal gap-1">
                                <Shield className="h-3 w-3" />
                                System
                              </Badge>
                            )}
                            {activeTab === "inherited" && role.ownerOrganizationName && (
                              <Badge variant="outline" className="text-xs font-normal">
                                Inherited from {role.ownerOrganizationName}
                              </Badge>
                            )}
                            {activeTab === "local" && scopeLabel && (
                              <Badge variant="secondary" className="text-xs font-normal">
                                {scopeLabel}
                              </Badge>
                            )}
                            <Badge variant="secondary" className="text-xs font-normal">
                              {role.industry}
                            </Badge>
                            <span className="text-xs text-muted-foreground">
                              {role.competencyCount} {role.competencyCount === 1 ? "competency" : "competencies"}
                            </span>
                          </div>
                        </div>
                        <div className="flex items-center gap-0.5 shrink-0 opacity-70 group-hover:opacity-100 transition-opacity">
                          {activeTab === "local" && (
                            <>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  updateUrl({ role: role.roleKey, view: "role", competency: null });
                                }}
                                title="Edit role"
                              >
                                <Pencil className="h-4 w-4" />
                              </Button>
                              <Button
                                variant="ghost"
                                size="icon"
                                className="h-8 w-8 text-muted-foreground hover:text-destructive"
                                onClick={(e) => {
                                  e.stopPropagation();
                                  setRoleToDelete(role);
                                }}
                                title="Delete role"
                              >
                                <Trash2 className="h-4 w-4" />
                              </Button>
                            </>
                          )}
                          {showClone && (
                            <Button
                              variant="ghost"
                              size="sm"
                              className="h-8 gap-1 text-xs"
                              disabled={isCloning === role.id}
                              onClick={(e) => {
                                e.stopPropagation();
                                handleCloneRole(role.id);
                              }}
                              title="Clone to this organization"
                            >
                              {isCloning === role.id ? (
                                <Loader2 className="h-3.5 w-3.5 animate-spin" />
                              ) : (
                                <Copy className="h-3.5 w-3.5" />
                              )}
                              Clone
                            </Button>
                          )}
                          <ChevronRight className="h-4 w-4 text-muted-foreground" />
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                );
              })}
            </div>
          )}

          <AlertDialog open={!!roleToDelete} onOpenChange={(open) => !open && setRoleToDelete(null)}>
            <AlertDialogContent>
              <AlertDialogHeader>
                <AlertDialogTitle>Delete role?</AlertDialogTitle>
                <AlertDialogDescription>
                  {roleToDelete && (
                    <>
                      &quot;{roleToDelete.roleName}&quot; and all its competencies will be permanently deleted. This action cannot be undone.
                    </>
                  )}
                </AlertDialogDescription>
              </AlertDialogHeader>
              <AlertDialogFooter>
                <AlertDialogCancel>Cancel</AlertDialogCancel>
                <AlertDialogAction
                  onClick={(e) => {
                    e.preventDefault();
                    confirmDeleteRole();
                  }}
                  className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                >
                  Delete role
                </AlertDialogAction>
              </AlertDialogFooter>
            </AlertDialogContent>
          </AlertDialog>
        </div>
      ) : (
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left column: competencies list (only when a role is selected) */}
        <div className="lg:col-span-1 space-y-4">
          {roleDetail && (
            <>
              <nav className="flex items-center gap-1 text-sm text-muted-foreground">
                <button
                  type="button"
                  className="hover:text-foreground"
                  onClick={() => updateUrl({ role: null, competency: null, view: null })}
                >
                  Roles
                </button>
                <span>/</span>
                <span className="text-foreground font-medium">{roleDetail.roleName}</span>
              </nav>
              <div className="flex items-center justify-between flex-wrap gap-2">
                <h2 className="text-lg font-semibold">Competencies</h2>
                <div className="flex items-center gap-2">
                  {roleDetail.competencies.length > 0 && (
                    <WeightTotal competencies={roleDetail.competencies} />
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    className="text-muted-foreground h-8 text-xs"
                    onClick={() => updateUrl({ role: roleKey!, competency: null, view: "rubric" })}
                  >
                    <ListChecks className="h-3 w-3 mr-1" />
                    Rubric (reference)
                  </Button>
                  {!isRoleReadOnly && (
                    <Button
                      size="sm"
                      className="h-8 text-xs gap-1.5"
                      onClick={handleGenerateCompetencies}
                      disabled={isGeneratingCompetencies || roleDetail.competencies.length >= 4}
                    >
                      {isGeneratingCompetencies ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                      ) : (
                        <Sparkles className="h-3.5 w-3.5" />
                      )}
                      Generate with AI
                    </Button>
                  )}
                  {isRoleReadOnly && (
                    <Badge variant="outline" className="text-xs font-normal">
                      Read-only
                    </Badge>
                  )}
                </div>
              </div>
              {isLoadingDetail ? (
                <div className="flex justify-center py-8">
                  <Loader2 className="h-8 w-8 animate-spin text-primary" />
                </div>
              ) : (
                <>
                  {/* Add competency at top */}
                  {!isRoleReadOnly && (
                    addCompetencyOpen ? (
                      <Card>
                        <CardContent className="py-4 space-y-3">
                          <Label>Competency name</Label>
                          <Input
                            value={newCompName}
                            onChange={(e) => setNewCompName(e.target.value)}
                            placeholder="e.g. Customer Service"
                          />
                          <Label>Description (optional)</Label>
                          <Textarea
                            value={newCompDescription}
                            onChange={(e) => setNewCompDescription(e.target.value)}
                            rows={2}
                            placeholder="What this competency measures"
                          />
                          <Label>Scoring weight (%)</Label>
                          <Input
                            type="number"
                            min={0}
                            max={100}
                            value={newCompWeight}
                            onChange={(e) => setNewCompWeight(parseInt(e.target.value, 10) || 0)}
                          />
                          <p className="text-xs text-muted-foreground">
                            All competency weights in this role must sum to exactly 100%.
                          </p>
                          <div className="flex gap-2">
                            <Button size="sm" variant="outline" onClick={() => setAddCompetencyOpen(false)}>
                              Cancel
                            </Button>
                            <Button
                              size="sm"
                              onClick={handleAddCompetency}
                              disabled={isAddingCompetency || !newCompName.trim()}
                            >
                              {isAddingCompetency ? <Loader2 className="h-4 w-4 animate-spin" /> : "Add"}
                            </Button>
                          </div>
                        </CardContent>
                      </Card>
                    ) : (
                      <Button variant="outline" className="w-full gap-2" onClick={() => setAddCompetencyOpen(true)}>
                        <Plus className="h-4 w-4" /> Add competency
                      </Button>
                    )
                  )}

                  <div className="space-y-2 mt-3">
                  {roleDetail.competencies
                    .sort((a, b) => a.displayOrder - b.displayOrder)
                    .map((comp) => {
                      const hasExample = !!(comp.canonicalExample?.trim());
                      return (
                        <Card
                          key={comp.id}
                          className={`overflow-hidden transition-colors ${
                            comp.id === competencyId && view === "questions"
                              ? "border-primary bg-primary/5"
                              : ""
                          }`}
                        >
                          <CardContent className="py-4">
                            <div className="flex items-start justify-between">
                              <div className="flex-1 min-w-0">
                                <p className="font-medium">{comp.name}</p>
                                <p className="text-xs text-muted-foreground mt-0.5">
                                  Behavioral scoring · {hasExample ? "Example set" : "No example"}
                                </p>
                              </div>
                              {!isRoleReadOnly ? (
                                <CompetencyWeightEditor
                                  competency={comp}
                                  roleTemplateId={roleDetail.id}
                                  onSaved={async () => {
                                    const detail = await fetchRoleTemplateDetail(roleDetail.roleKey);
                                    setRoleDetail(detail);
                                  }}
                                />
                              ) : (
                                <Badge variant="outline" className="ml-2 shrink-0 text-xs font-mono">
                                  {comp.defaultWeight}%
                                </Badge>
                              )}
                            </div>
                            <div className="flex flex-wrap gap-1.5 mt-3">
                              <Button
                                size="sm"
                                variant="outline"
                                className="h-7 text-xs"
                                onClick={() =>
                                  updateUrl({
                                    role: roleKey!,
                                    competency: comp.id,
                                    view: "questions",
                                  })
                                }
                              >
                                <FileQuestion className="h-3 w-3 mr-1" /> Example question
                              </Button>
                              {!isRoleReadOnly && (
                                <Button
                                  size="sm"
                                  variant="ghost"
                                  className="h-7 text-xs text-muted-foreground hover:text-destructive ml-auto"
                                  onClick={(e) => {
                                    e.stopPropagation();
                                    setCompToDelete(comp);
                                  }}
                                  title="Delete competency"
                                >
                                  <Trash2 className="h-3 w-3 mr-1" /> Delete
                                </Button>
                              )}
                            </div>
                          </CardContent>
                        </Card>
                      );
                    })}
                  </div>
              {roleDetail.competencies.length === 0 && !isLoadingDetail && (
                <div className="space-y-3">
                  <p className="text-sm text-muted-foreground">
                    {isRoleReadOnly
                      ? "This role has no competencies."
                      : "No competencies yet. Add one manually or generate suggested competencies for this role."}
                  </p>
                  {!isRoleReadOnly && (
                    <Button
                      variant="outline"
                      className="w-full gap-2"
                      onClick={handleGenerateCompetencies}
                      disabled={isGeneratingCompetencies}
                    >
                      {isGeneratingCompetencies ? (
                        <Loader2 className="h-4 w-4 animate-spin" />
                      ) : (
                        <Sparkles className="h-4 w-4" />
                      )}
                      Generate competencies with AI
                    </Button>
                  )}
                </div>
              )}
            </>
              )}

              <AlertDialog open={!!compToDelete} onOpenChange={(open) => !open && setCompToDelete(null)}>
                <AlertDialogContent>
                  <AlertDialogHeader>
                    <AlertDialogTitle>Delete competency?</AlertDialogTitle>
                    <AlertDialogDescription>
                      {compToDelete && (
                        <>
                          &quot;{compToDelete.name}&quot; will be removed from this role. Weights of other competencies will not change. This action cannot be undone.
                        </>
                      )}
                    </AlertDialogDescription>
                  </AlertDialogHeader>
                  <AlertDialogFooter>
                    <AlertDialogCancel>Cancel</AlertDialogCancel>
                    <AlertDialogAction
                      onClick={(e) => {
                        e.preventDefault();
                        confirmDeleteCompetency();
                      }}
                      className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
                    >
                      Delete competency
                    </AlertDialogAction>
                  </AlertDialogFooter>
                </AlertDialogContent>
              </AlertDialog>
            </>
          )}
        </div>

        {/* Right panel */}
        <div className="lg:col-span-2">
          {view === "role" && roleKey && roleDetail && (
            <RoleEditPanel
              roleDetail={roleDetail}
              readOnly={isRoleReadOnly}
              onSaved={() => fetchRoleTemplateDetail(roleKey).then(setRoleDetail)}
              onDeleted={() => {
                setRoleDetail(null);
                updateUrl({ role: null, competency: null, view: null });
                loadRoles();
                router.push("/interview-content");
              }}
            />
          )}
          {view === "rubric" && roleDetail && (
            <UniversalRubricPanel />
          )}
          {view === "questions" && roleDetail && selectedCompetency && (
            <CanonicalExamplePanel
              competency={selectedCompetency}
              roleTemplateId={roleDetail.id}
              roleKey={roleKey!}
              roleContext={`${roleDetail.roleName} (${roleDetail.industry})`}
              onSaved={() => fetchRoleTemplateDetail(roleKey!).then(setRoleDetail)}
            />
          )}
          {!view && (
            <Card>
              <CardContent className="py-16 text-center">
                <p className="text-muted-foreground">
                  Choose Example question on a competency. Use &quot;Rubric (reference)&quot; in the left column to view the Universal Rubric — Behavioral Competency Scoring.
                </p>
              </CardContent>
            </Card>
          )}
          {view === "questions" && !selectedCompetency && (
            <Card>
              <CardContent className="py-16 text-center">
                <p className="text-muted-foreground">Select a competency to set its example question.</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>
      )}
    </div>
  );
}

function RoleEditPanel({
  roleDetail,
  readOnly,
  onSaved,
  onDeleted,
}: {
  roleDetail: RoleTemplateDetail;
  readOnly: boolean;
  onSaved: () => void;
  onDeleted: () => void;
}) {
  const [roleName, setRoleName] = useState(roleDetail.roleName);
  const [industry, setIndustry] = useState(roleDetail.industry);
  const [visScope, setVisScope] = useState(roleDetail.visibilityScope ?? "organization_only");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setRoleName(roleDetail.roleName);
    setIndustry(roleDetail.industry);
    setVisScope(roleDetail.visibilityScope ?? "organization_only");
  }, [roleDetail.roleName, roleDetail.industry, roleDetail.visibilityScope]);

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);
    try {
      await updateRoleTemplate(roleDetail.id, { roleName, industry, visibilityScope: visScope });
      onSaved();
    } catch (err) {
      console.error(err);
      setError("Failed to save.");
    } finally {
      setIsSaving(false);
    }
  };

  if (readOnly) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            {roleDetail.source === "system" && <Shield className="h-5 w-5" />}
            {roleDetail.roleName}
          </CardTitle>
          <p className="text-sm text-muted-foreground">
            {roleDetail.source === "system"
              ? "System role templates cannot be edited."
              : roleDetail.isInherited
                ? `Inherited role from ${roleDetail.ownerOrganizationName ?? "a parent organization"}. Clone it to edit.`
                : "This role is read-only."}
          </p>
        </CardHeader>
        <CardContent className="space-y-2">
          <div className="text-sm"><span className="font-medium">Industry:</span> {roleDetail.industry}</div>
          <div className="text-sm"><span className="font-medium">Competencies:</span> {roleDetail.competencies.length}</div>
          {roleDetail.visibilityScope && (
            <div className="text-sm"><span className="font-medium">Visibility:</span> {snakeToTitleCase(roleDetail.visibilityScope)}</div>
          )}
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Edit role</CardTitle>
        <p className="text-sm text-muted-foreground">Update role name, industry, and visibility.</p>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && <p className="text-sm text-destructive">{error}</p>}
        <div>
          <Label>Role name</Label>
          <Input value={roleName} onChange={(e) => setRoleName(e.target.value)} />
        </div>
        <div>
          <Label>Industry</Label>
          <Input value={industry} onChange={(e) => setIndustry(e.target.value)} />
        </div>
        <div className="space-y-2">
          <Label>Visibility</Label>
          {[
            { value: "organization_only", label: "This organization only", desc: "Only visible at the current organization." },
            { value: "organization_and_descendants", label: "This organization and sub-organizations", desc: "Visible here and at all sub-organizations below." },
            { value: "descendants_only", label: "Sub-organizations only", desc: "Only visible at sub-organizations, not at this organization." },
          ].map((opt) => (
            <label
              key={opt.value}
              className={`flex items-start gap-3 rounded-lg border p-3 cursor-pointer transition-colors ${
                visScope === opt.value
                  ? "border-primary bg-primary/5"
                  : "border-border hover:bg-muted/50"
              }`}
            >
              <input
                type="radio"
                name="editVisibility"
                value={opt.value}
                checked={visScope === opt.value}
                onChange={(e) => setVisScope(e.target.value)}
                className="accent-primary mt-0.5"
              />
              <div>
                <p className="text-sm font-medium">{opt.label}</p>
                <p className="text-xs text-muted-foreground">{opt.desc}</p>
              </div>
            </label>
          ))}
        </div>
        <div className="flex gap-2">
          <Button onClick={handleSave} disabled={isSaving} className="gap-2">
            {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
            Save
          </Button>
          <Button
            variant="outline"
            className="text-destructive hover:text-destructive"
            onClick={async () => {
              if (!confirm("Delete this role and all its contents?")) return;
              try {
                await deleteRoleTemplate(roleDetail.id);
                onDeleted();
              } catch (err) {
                console.error(err);
                setError("Failed to delete.");
              }
            }}
          >
            <Trash2 className="h-4 w-4 mr-1" /> Delete
          </Button>
        </div>
      </CardContent>
    </Card>
  );
}

function UniversalRubricPanel() {
  const [levels, setLevels] = useState<UniversalRubricLevel[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      try {
        setError(null);
        const data = await fetchUniversalRubric();
        setLevels(data);
      } catch (err) {
        console.error(err);
        setError("Failed to load rubric.");
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  return (
    <Card>
      <CardHeader>
        <CardTitle>Universal Rubric — Behavioral Competency Scoring</CardTitle>
        <p className="text-sm text-muted-foreground">
          Each competency is scored 1-5 based on the overall quality of behavioral evidence the candidate provided. Scores are assigned after the candidate answers the primary question and any follow-ups.
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && <p className="text-sm text-destructive">{error}</p>}
        {loading ? (
          <div className="flex justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
          </div>
        ) : (
          <ul className="space-y-2">
            {levels.map((level) => (
              <li key={level.level} className="flex items-start gap-2 rounded-lg border p-3">
                <span className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-muted text-sm font-bold">{level.level}</span>
                <div>
                  <p className="font-medium">{level.label}</p>
                  <p className="text-sm text-muted-foreground">{level.description}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}

function CanonicalExamplePanel({
  competency,
  roleTemplateId,
  roleKey,
  roleContext,
  onSaved,
}: {
  competency: Competency;
  roleTemplateId: string;
  roleKey: string;
  roleContext: string;
  onSaved: () => void;
}) {
  const [value, setValue] = useState(competency.canonicalExample ?? "");
  const [isSaving, setIsSaving] = useState(false);
  const [isGenerating, setIsGenerating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setValue(competency.canonicalExample ?? "");
  }, [competency.id, competency.canonicalExample]);

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);
    try {
      await updateCompetency(competency.id, roleTemplateId, {
        name: competency.name,
        description: competency.description ?? undefined,
        canonicalExample: value.trim() || "e.g. Tell me about a time when you demonstrated this.",
        defaultWeight: competency.defaultWeight,
        isRequired: competency.isRequired,
        displayOrder: competency.displayOrder,
      });
      onSaved();
    } catch (err) {
      console.error(err);
      setError("Failed to save.");
    } finally {
      setIsSaving(false);
    }
  };

  const handleGenerate = async () => {
    setIsGenerating(true);
    setError(null);
    try {
      const suggested = await aiSuggestCanonicalExample(competency.name, roleContext, competency.description);
      setValue(suggested || value);
    } catch (err) {
      console.error(err);
      setError("Failed to generate suggestion.");
    } finally {
      setIsGenerating(false);
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>Example question — {competency.name}</CardTitle>
        <p className="text-sm text-muted-foreground">
          The AI uses this as a model, not a script. Write the question you&apos;d want a great interviewer to ask.
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        {error && <p className="text-sm text-destructive">{error}</p>}
        <div className="flex gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={handleGenerate}
            disabled={isGenerating}
          >
            {isGenerating ? <Loader2 className="h-4 w-4 animate-spin" /> : <Sparkles className="h-4 w-4" />}
            Generate with AI
          </Button>
        </div>
        <Textarea
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder="e.g. When you finish a task, how do you know it's actually done right?"
          rows={4}
          className="resize-none"
        />
        <Button onClick={handleSave} disabled={isSaving} className="gap-2">
          {isSaving ? <Loader2 className="h-4 w-4 animate-spin" /> : <Save className="h-4 w-4" />}
          Save
        </Button>
      </CardContent>
    </Card>
  );
}

function WeightTotal({ competencies }: { competencies: Competency[] }) {
  const total = competencies.reduce((sum, c) => sum + (c.defaultWeight ?? 0), 0);
  const isValid = total === 100;
  return (
    <Badge variant={isValid ? "default" : "destructive"} className="text-xs font-mono">
      {total} / 100%
    </Badge>
  );
}

function CompetencyWeightEditor({
  competency,
  roleTemplateId,
  onSaved,
}: {
  competency: Competency;
  roleTemplateId: string;
  onSaved: () => Promise<void>;
}) {
  const [editing, setEditing] = useState(false);
  const [weight, setWeight] = useState(competency.defaultWeight);
  const [weightInput, setWeightInput] = useState(String(competency.defaultWeight));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setWeight(competency.defaultWeight);
    setWeightInput(String(competency.defaultWeight));
  }, [competency.defaultWeight]);

  const handleSave = async () => {
    setSaving(true);
    try {
      await updateCompetency(competency.id, roleTemplateId, {
        name: competency.name,
        description: competency.description ?? undefined,
        canonicalExample: competency.canonicalExample ?? "e.g. Tell me about a time when you demonstrated this.",
        defaultWeight: weight,
        isRequired: competency.isRequired,
        displayOrder: competency.displayOrder,
      });
      setEditing(false);
      await onSaved();
    } catch (err) {
      console.error(err);
    } finally {
      setSaving(false);
    }
  };

  if (editing) {
    return (
      <div className="flex items-center gap-2 ml-2 shrink-0 rounded-md border border-input bg-background px-2 py-0.5 focus-within:ring-2 focus-within:ring-ring focus-within:ring-offset-1">
        <span className="text-xs text-muted-foreground whitespace-nowrap">Weight</span>
        <Input
          type="number"
          min={0}
          max={100}
          className="w-12 h-6 border-0 p-0 text-xs text-right bg-transparent focus-visible:ring-0 focus-visible:ring-offset-0 [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
          value={weightInput}
          onChange={(e) => {
            const raw = e.target.value;
            if (raw === "") {
              setWeight(0);
              setWeightInput("");
              return;
            }
            const v = parseInt(raw, 10);
            if (!Number.isNaN(v)) {
              const clamped = Math.min(100, Math.max(0, v));
              setWeight(clamped);
              setWeightInput(String(clamped));
            }
          }}
          onKeyDown={(e) => {
            if (e.key === "Enter") handleSave();
            if (e.key === "Escape") {
              setWeight(competency.defaultWeight);
              setWeightInput(String(competency.defaultWeight));
              setEditing(false);
            }
          }}
          autoFocus
        />
        <span className="text-xs text-muted-foreground tabular-nums">%</span>
        <Button size="sm" variant="ghost" className="h-6 w-6 p-0 shrink-0" onClick={handleSave} disabled={saving}>
          {saving ? <Loader2 className="h-3 w-3 animate-spin" /> : <Save className="h-3 w-3" />}
        </Button>
      </div>
    );
  }

  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      className="ml-2 shrink-0 h-7 gap-1 text-xs font-mono"
      onClick={() => setEditing(true)}
      title="Edit scoring weight"
    >
      <span className="text-muted-foreground">Weight</span>
      <span>{competency.defaultWeight}%</span>
      <Pencil className="h-3 w-3 text-muted-foreground" />
    </Button>
  );
}
