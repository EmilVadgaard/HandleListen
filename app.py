from flask import Flask, render_template, request, redirect, url_for

from flask_sqlalchemy import SQLAlchemy

from wtforms import Form, SubmitField, StringField, IntegerField, SelectField
from wtforms.validators import DataRequired,Length

app = Flask("app")

# configure SQL Alchemy ORM to use sqlite3 file flask.db
app.config['SQLALCHEMY_DATABASE_URI'] = "sqlite:///flask.db"
app.config['WTF_CSRF_ENABLED'] = False # warning!
app.config['SECRET_KEY'] = "change-me"

db = SQLAlchemy(app)


# Data Model ------------------------------------------------------------------

class RecipeToIngredient(db.Model):
    __tablename__ = 'recipe_to_ingredient'

    recipe_id   = db.Column(db.Integer, db.ForeignKey('recipes.id'), primary_key=True)
    ingredient_id = db.Column(db.Integer, db.ForeignKey('ingredients.id'), primary_key=True)
    amount = db.Column(db.Float, nullable=False)
    unit = db.Column(db.String(20), nullable=False)

    recipe = db.relationship('Recipe', back_populates='ingredient_links')
    ingredient = db.relationship('Ingredient', back_populates='recipe_links')

class Recipe(db.Model):
    __tablename__ = 'recipes'

    id   = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    ingredient_links = db.relationship('RecipeToIngredient', back_populates='recipe', cascade="all, delete-orphan")

class Ingredient(db.Model):
    __tablename__ = 'ingredients'

    id   = db.Column(db.Integer, primary_key=True)
    name = db.Column(db.String(100), nullable=False)
    recipe_links = db.relationship('RecipeToIngredient', back_populates='ingredient', cascade="all, delete-orphan")

with app.app_context():
  # empty the database
  db.drop_all()
  # ensure the schema includes this data model
  db.create_all()

# Forms -----------------------------------------------------------------------
class AddIngredientToRecipeForm(Form):
  ingredient = SelectField( label='Ingredient', coerce=int )
  amount = IntegerField( label='Amount', validators=[DataRequired()], default = 0 )
  unit = SelectField( label='Unit', choices=[('g', 'grams'), ('kg', 'kilograms'), ('ml', 'milliliters'), ('l', 'liters'), ('tsp', 'teaspoon'), ('tbsp', 'tablespoon'), ('cup', 'cup'), ('pcs', 'pieces')] )
  
  submit = SubmitField('Add Ingredient to Recipe')

class RecipeForm(Form):
  name = StringField( label='Recipe Name', validators=[DataRequired(), Length(min=1, max=100) ] )
  
  submit = SubmitField('Add Recipe')

class IngredientForm(Form):
  parent: RecipeForm
  name = StringField( label='Ingredient Name', validators=[DataRequired(), Length(min=1, max=100) ] )
  
  submit = SubmitField('Add Ingredient')

# Routes ----------------------------------------------------------------------

def add_ingredient(name:str):
    new_ingredient = Ingredient(name=name)
    db.session.add(new_ingredient)
    db.session.commit()

def add_recipe(name:str):
    new_recipe = Recipe(name=name)
    db.session.add(new_recipe)
    db.session.commit()

def link_ingredient_to_recipe(recipe_id:int, ingredient_id:int, amount:float, unit:str):
    link = RecipeToIngredient(recipe_id=recipe_id, ingredient_id=ingredient_id, amount=amount, unit=unit)
    db.session.add(link)
    db.session.commit()

def link_ingredient_to_recipe_by_names(recipe_name:str, ingredient_name:str, amount:float, unit:str):
    recipe = Recipe.query.filter_by(name=recipe_name).first()
    ingredient = Ingredient.query.filter_by(name=ingredient_name).first()
    if recipe and ingredient:
        link_ingredient_to_recipe(recipe.id, ingredient.id, amount, unit)

@app.route('/', methods=['GET', 'POST'])
def home():
    # Get data from database
    recipes = Recipe.query.all()
    ingredients = Ingredient.query.all()

    # Create forms
    recipeForm = RecipeForm(request.form)
    ingredientForm = IngredientForm(request.form)
    AddIngredientToRecipeForm = AddIngredientToRecipeForm(request.form)
    if request.method == 'POST' and recipeForm.validate() and ingredientForm.validate() and AddIngredientToRecipeForm.validate():
        recipe_name = recipeForm.name.data
        add_recipe(recipe_name)

        ingredient_name = ingredientForm.name.data
        add_ingredient(ingredient_name)


        amount = AddIngredientToRecipeForm.amount.data
        unit = AddIngredientToRecipeForm.unit.data
        

    recipes = Recipe.query.order_by(Recipe.id).all()
    return render_template('index.html', recipes=recipes, form=form)

